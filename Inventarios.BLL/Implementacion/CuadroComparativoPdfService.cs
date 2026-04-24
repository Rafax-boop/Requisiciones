using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class CuadroComparativoPdfService : ICuadroComparativoPdfService
    {
        private readonly IRequisicionesService _requisicionesService;
        private readonly IProveedoresService _cotizacionesService;

        public CuadroComparativoPdfService(
            IRequisicionesService requisicionesService,
            IProveedoresService cotizacionesService)
        {
            _requisicionesService = requisicionesService;
            _cotizacionesService = cotizacionesService;
        }

        public async Task<(byte[] PdfBytes, string FileName)?> GenerarAsync(
            int idRequisicion,
            string webRootPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(idRequisicion);
            if (dto == null) return null;

            dto.Articulos = await _cotizacionesService.ObtenerArticulosParaCompra(idRequisicion);

            var cotizaciones = await _cotizacionesService.ObtenerCotizaciones(idRequisicion);

            var ganador = await _cotizacionesService.ObtenerProveedorGanador(idRequisicion);
            var idProveedorGanado = ganador?.IdProveedor ?? 0;

            // Agrupa cotizaciones por IdPartida (IdRequiDetalle), luego por proveedor
            // Resultado: dict[idPartida] = lista de cotizaciones de ese artículo
            var cotsPorPartida = cotizaciones
                .GroupBy(c => c.IdPartida)
                .ToDictionary(g => g.Key, g => g.ToList());

            // Extrae hasta 3 proveedores distintos (en el orden en que aparecen)
            var proveedores = cotizaciones
                .Select(c => new { c.IdProveedor, c.NombreProveedor })
                .DistinctBy(p => p.IdProveedor)
                .OrderBy(p => p.IdProveedor == idProveedorGanado ? 0 : 1)
                .Take(3)
                .ToList();

            var indiceGanadorForzado = idProveedorGanado > 0
                ? proveedores.FindIndex(p => p.IdProveedor == idProveedorGanado)
                : -1;

            var filas = dto.Articulos?
                .Select(a =>
                {
                    var cantidadTexto = a.Cantidad.HasValue
                        ? decimal.Truncate(a.Cantidad.Value) == a.Cantidad.Value
                            ? ((int)a.Cantidad.Value).ToString(CultureInfo.InvariantCulture)
                            : a.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture)
                        : "";

                    // Busca las cotizaciones de esta partida usando el Id real (no se muestra)
                    var cotsDeLaPartida = cotsPorPartida.TryGetValue(a.IdRequisicionDetalle, out var lista)
                        ? lista
                        : new List<CotizacionDTO>();

                    // Mapea hasta 3 precios según el orden de proveedores detectado arriba
                    var precios = proveedores
                        .Select(p => cotsDeLaPartida
                            .FirstOrDefault(c => c.IdProveedor == p.IdProveedor))
                        .ToArray();

                    return new CuadroComparativoFilaPdf
                    {
                        Partida = a.NumPartida?.ToString() ?? "",
                        Descripcion = a.DescripcionDetallada ?? a.Descripcion ?? "",
                        Cantidad = a.Cantidad ?? 0m,
                        CantidadTxt = cantidadTexto,
                        Unidad = a.UnidadMedida ?? "",
                        PrecioP1 = precios.ElementAtOrDefault(0)?.Importe,
                        PrecioP2 = precios.ElementAtOrDefault(1)?.Importe,
                        PrecioP3 = precios.ElementAtOrDefault(2)?.Importe,
                        IvaP1 = precios.ElementAtOrDefault(0)?.IVA ?? false,
                        IvaP2 = precios.ElementAtOrDefault(1)?.IVA ?? false,
                        IvaP3 = precios.ElementAtOrDefault(2)?.IVA ?? false,
                    };
                })
                .ToList() ?? new List<CuadroComparativoFilaPdf>();

            var nombresProveedores = proveedores.Select(p => p.NombreProveedor).ToArray();

            var bytes = GenerarCuadroComparativoPdf(
                webRootPath,
                requisicion: dto.NumRequisicion ?? $"REQ-{idRequisicion}",
                fecha: dto.FechaEmision?.ToDateTime(TimeOnly.MinValue),
                departamento: dto.Departamento ?? "",
                justificacion: dto.Justificacion ?? "",
                filas: filas,
                nombresProveedores: nombresProveedores,
                indiceGanadorForzado: indiceGanadorForzado);

            var nombreArchivo =
                $"CuadroComparativo_{(dto.NumRequisicion ?? idRequisicion.ToString()).Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (bytes, nombreArchivo);
        }

        // ─────────────────────────────────────────────────────────────────────
        private static byte[] GenerarCuadroComparativoPdf(
            string webRootPath,
            string requisicion,
            DateTime? fecha,
            string departamento,
            string justificacion,
            List<CuadroComparativoFilaPdf> filas,
            string[] nombresProveedores,
            int indiceGanadorForzado = -1)
        {
            const decimal tasaIva = 0.16m;

            // Pre-calcula totales por proveedor (suma de importe*cantidad)
            decimal SubtotalProveedor(int idx) => filas.Sum(f =>
            {
                var precio = idx == 0 ? f.PrecioP1 : idx == 1 ? f.PrecioP2 : f.PrecioP3;
                if (!precio.HasValue) return 0m;
                return precio.Value * f.Cantidad;
            });

            decimal IvaProveedor(int idx) => filas.Sum(f =>
            {
                var precio = idx == 0 ? f.PrecioP1 : idx == 1 ? f.PrecioP2 : f.PrecioP3;
                var tieneIva = idx == 0 ? f.IvaP1 : idx == 1 ? f.IvaP2 : f.IvaP3;
                if (!precio.HasValue || !tieneIva) return 0m;
                return precio.Value * f.Cantidad * tasaIva;
            });

            var sumas = new[] { SubtotalProveedor(0), SubtotalProveedor(1), SubtotalProveedor(2) };
            var ivas = new[] { IvaProveedor(0), IvaProveedor(1), IvaProveedor(2) };
            var totals = sumas.Select((s, i) => s + ivas[i]).ToArray();

            // Proveedor seleccionado: el de menor total (solo entre los que tienen datos)
            int indiceGanador;
            if (indiceGanadorForzado >= 0 && indiceGanadorForzado < 3 && totals[indiceGanadorForzado] > 0)
            {
                // Usar el ganador seleccionado por el usuario
                indiceGanador = indiceGanadorForzado;
            }
            else
            {
                // Fallback: menor total (si no hay ganador guardado o el índice no tiene datos)
                indiceGanador = -1;
                var menorTotal = decimal.MaxValue;
                for (var i = 0; i < 3; i++)
                {
                    if (totals[i] > 0 && totals[i] < menorTotal)
                    {
                        menorTotal = totals[i];
                        indiceGanador = i;
                    }
                }
            }

            // ── helpers de formato ──────────────────────────────────────────
            static string Moneda(decimal v) => v == 0 ? "" : v.ToString("C2", new CultureInfo("es-MX"));
            static string MonedaFmt(decimal? v) => v.HasValue && v.Value != 0
                ? v.Value.ToString("C2", new CultureInfo("es-MX"))
                : "";

            var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(18f, 16f, 18f, 16f);

            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var fondoEncabezado = new DeviceRgb(248, 250, 252);
            var textoPrincipal = new DeviceRgb(26, 26, 26);
            var textoSecundario = new DeviceRgb(100, 116, 139);
            var textoEncabezado = new DeviceRgb(71, 85, 105);
            var textoInstitucional = new DeviceRgb(45, 45, 45);
            var rosaAcento = new DeviceRgb(255, 45, 111);
            var verdeGanador = new DeviceRgb(220, 252, 231); // fondo verde suave
            var verdeTexto = new DeviceRgb(22, 101, 52);  // texto verde oscuro
            var borde = new SolidBorder(new DeviceRgb(226, 232, 240), 0.85f);

            Cell CellHead(string text, int colspan = 1, int rowspan = 1,
                          TextAlignment align = TextAlignment.LEFT,
                          DeviceRgb bgOverride = null, DeviceRgb fgOverride = null)
                => new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetBackgroundColor(bgOverride ?? fondoEncabezado)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text)
                        .SetFont(fontBold)
                        .SetFontSize(6.6f)
                        .SetFontColor(fgOverride ?? textoEncabezado));

            Cell CellBody(string text, int colspan = 1, int rowspan = 1,
                          TextAlignment align = TextAlignment.LEFT, float minHeight = 0f,
                          DeviceRgb bgOverride = null, DeviceRgb fgOverride = null)
            {
                var c = new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text)
                        .SetFont(fontRegular)
                        .SetFontSize(6.8f)
                        .SetFontColor(fgOverride ?? textoPrincipal));
                if (bgOverride != null) c.SetBackgroundColor(bgOverride);
                if (minHeight > 0f) c.SetMinHeight(minHeight);
                return c;
            }

            // ── ENCABEZADO INSTITUCIONAL ────────────────────────────────────
            var header = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 64f, 18f }))
                .UseAllAvailableWidth();
            header.SetBorder(borde);

            var logoIzq = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoIzq = System.IO.Path.Combine(webRootPath, "img", "corazon.png");
            if (File.Exists(rutaLogoIzq))
                logoIzq.Add(new Image(ImageDataFactory.Create(rutaLogoIzq))
                    .ScaleToFit(90f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                logoIzq.Add(new Paragraph("PUEBLA").SetFont(fontBold).SetFontSize(10f)
                    .SetTextAlignment(TextAlignment.CENTER));
            header.AddCell(logoIzq);

            var centro = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(6f);
            centro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA DEL ESTADO DE PUEBLA")
                .SetFont(fontBold).SetFontSize(7.2f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DIRECCION DE ADMINISTRACION Y FINANZAS")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("CUADRO COMPARATIVO")
                .SetFont(fontBold).SetFontSize(10f).SetFontColor(rosaAcento).SetMarginTop(4f));
            header.AddCell(centro);

            var logoDer = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoDer = System.IO.Path.Combine(webRootPath, "img", "familias-dif-rosa.png");
            if (File.Exists(rutaLogoDer))
                logoDer.Add(new Image(ImageDataFactory.Create(rutaLogoDer))
                    .ScaleToFit(95f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                logoDer.Add(new Paragraph("Familias").SetFont(fontBold).SetFontSize(10f)
                    .SetTextAlignment(TextAlignment.CENTER));
            header.AddCell(logoDer);
            doc.Add(header);
            doc.Add(new Paragraph(" ").SetMargin(2f));

            // ── INFO REQUISICION ────────────────────────────────────────────
            var fechaTxt = fecha.HasValue
                ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : "";
            var info = new Table(UnitValue.CreatePercentArray(new float[] { 20f, 58f, 10f, 12f }))
                .UseAllAvailableWidth();
            info.AddCell(CellHead("SECCION DE ADQUISICIONES", 4));
            info.AddCell(CellHead("TIPO DE PROCEDIMIENTO:"));
            info.AddCell(CellBody("PROCEDIMIENTO DE ADJUDICACION (PENDIENTE DE CAPTURA)", 3));
            info.AddCell(CellHead("CRITERIO DE ADJUDICACION:"));
            info.AddCell(CellBody("SELECCION AL PROVEEDOR QUE CUMPLA CON REQUISITOS LEGALES Y OFERTE EL PRECIO MAS BAJO.", 3));
            info.AddCell(CellHead("REQUISICION"));
            info.AddCell(CellBody(requisicion, 1, 1, TextAlignment.CENTER));
            info.AddCell(CellHead("FECHA:", 1, 1, TextAlignment.CENTER));
            info.AddCell(CellBody(fechaTxt, 1, 1, TextAlignment.CENTER));
            info.AddCell(CellHead("JUSTIFICACION:"));
            info.AddCell(CellBody(string.IsNullOrWhiteSpace(justificacion) ? " " : justificacion, 3));
            info.AddCell(CellHead("AREAS SOLICITANTES:"));
            info.AddCell(CellBody(string.IsNullOrWhiteSpace(departamento) ? " " : departamento, 3));
            doc.Add(info);
            doc.Add(new Paragraph(" ").SetMargin(2f));

            // ── TABLA COMPARATIVA ───────────────────────────────────────────
            // Nombres de proveedores en encabezado (o genérico si no hay)
            string NombreEnc(int i) => i < nombresProveedores.Length && !string.IsNullOrWhiteSpace(nombresProveedores[i])
                ? nombresProveedores[i]
                : $"PROVEEDOR {i + 1}";

            var tabla = new Table(UnitValue.CreatePercentArray(
                new float[] { 7f, 31f, 7f, 7f, 8f, 8f, 8f, 8f, 8f, 8f }))
                .UseAllAvailableWidth();

            tabla.AddHeaderCell(CellHead("PARTIDA", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("DESCRIPCION", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("CANTIDAD", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("U.M.", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead(NombreEnc(0), 2, 1, TextAlignment.CENTER,
                indiceGanador == 0 ? verdeGanador : null,
                indiceGanador == 0 ? verdeTexto : null));
            tabla.AddHeaderCell(CellHead(NombreEnc(1), 2, 1, TextAlignment.CENTER,
                indiceGanador == 1 ? verdeGanador : null,
                indiceGanador == 1 ? verdeTexto : null));
            tabla.AddHeaderCell(CellHead(NombreEnc(2), 2, 1, TextAlignment.CENTER,
                indiceGanador == 2 ? verdeGanador : null,
                indiceGanador == 2 ? verdeTexto : null));
            // Segunda fila de encabezado: P/UNITARIO | TOTAL × 3
            for (var p = 0; p < 3; p++)
            {
                var esGanador = indiceGanador == p;
                tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER,
                    esGanador ? verdeGanador : null, esGanador ? verdeTexto : null));
                tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER,
                    esGanador ? verdeGanador : null, esGanador ? verdeTexto : null));
            }

            // Filas de artículos
            var totalFilas = filas.Count; ;
            for (var i = 0; i < totalFilas; i++)
            {
                var fila = i < filas.Count ? filas[i] : new CuadroComparativoFilaPdf();
                var fondoFila = i % 2 == 1 ? fondoEncabezado : ColorConstants.WHITE;

                decimal? Total(decimal? precio) =>
                    precio.HasValue && fila.Cantidad > 0 ? precio.Value * fila.Cantidad : null;

                tabla.AddCell(CellBody(
                    string.IsNullOrWhiteSpace(fila.Partida) ? (i + 1).ToString() : fila.Partida,
                    align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila));
                tabla.AddCell(CellBody(fila.Descripcion ?? "",
                    minHeight: 17f).SetBackgroundColor(fondoFila));
                tabla.AddCell(CellBody(fila.CantidadTxt ?? "",
                    align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila));
                tabla.AddCell(CellBody(fila.Unidad ?? "",
                    align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila));

                var precios = new[] { fila.PrecioP1, fila.PrecioP2, fila.PrecioP3 };
                for (var p = 0; p < 3; p++)
                {
                    var esGanador = indiceGanador == p;
                    var bgFila = esGanador ? verdeGanador : fondoFila;
                    tabla.AddCell(CellBody(MonedaFmt(precios[p]),
                        align: TextAlignment.RIGHT, minHeight: 17f, bgOverride: (DeviceRgb)bgFila));
                    tabla.AddCell(CellBody(MonedaFmt(Total(precios[p])),
                        align: TextAlignment.RIGHT, minHeight: 17f, bgOverride: (DeviceRgb)bgFila));
                }
            }

            // Filas SUMA / IVA / TOTAL
            var etiquetasResumen = new[] { ("SUMA", sumas), ("IVA", ivas), ("TOTAL", totals) };
            foreach (var (etiqueta, valores) in etiquetasResumen)
            {
                tabla.AddCell(CellBody("", 4));
                for (var p = 0; p < 3; p++)
                {
                    var esGanador = indiceGanador == p;
                    var bg = esGanador ? verdeGanador : null;
                    var fg = esGanador ? verdeTexto : null;
                    tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER, bg, fg));

                    // ✅ Solo este cambio: guión si IVA es 0
                    var texto = (etiqueta == "IVA" && valores[p] == 0) ? "—" : Moneda(valores[p]);
                    tabla.AddCell(CellBody(texto, align: TextAlignment.RIGHT, bgOverride: bg, fgOverride: fg));
                }
            }

            // Condiciones de pago
            tabla.AddCell(CellHead("CONDICIONES DE PAGO", 4, 1, TextAlignment.CENTER));
            for (var p = 0; p < 3; p++)
            {
                var esGanador = indiceGanador == p;
                tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER,
                    bgOverride: esGanador ? verdeGanador : null,
                    fgOverride: esGanador ? verdeTexto : null));
            }

            doc.Add(tabla);
            doc.Add(new Paragraph(" ").SetMargin(3f));

            // ── PROVEEDOR SELECCIONADO ──────────────────────────────────────────
            var nombreGanador = indiceGanador >= 0 && indiceGanador < nombresProveedores.Length
                ? nombresProveedores[indiceGanador]
                : "";

            var bgGanador = indiceGanador >= 0 ? verdeGanador : null;
            var fgGanador = indiceGanador >= 0 ? verdeTexto : null;

            var proveedor = new Table(UnitValue.CreatePercentArray(
                new float[] { 55f, 15f, 15f, 15f }))  // 4 columnas exactas
                .UseAllAvailableWidth();

            // ── fila 1: encabezados ──
            proveedor.AddCell(CellHead("PROVEEDOR(ES) SELECCIONADO(S)", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("SUMA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("IVA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("SUBTOTAL", align: TextAlignment.CENTER));

            // ── fila 2: datos ──
            proveedor.AddCell(CellBody(nombreGanador,
                bgOverride: bgGanador, fgOverride: fgGanador));
            proveedor.AddCell(CellBody(indiceGanador >= 0 ? Moneda(sumas[indiceGanador]) : "",
                align: TextAlignment.RIGHT, bgOverride: bgGanador, fgOverride: fgGanador));

            // ✅ Guión si el IVA del ganador es 0
            var textoIvaGanador = indiceGanador >= 0
                ? (ivas[indiceGanador] == 0 ? "—" : Moneda(ivas[indiceGanador]))
                : "";
            proveedor.AddCell(CellBody(textoIvaGanador,
                align: TextAlignment.RIGHT, bgOverride: bgGanador, fgOverride: fgGanador));

            proveedor.AddCell(CellBody(indiceGanador >= 0 ? Moneda(totals[indiceGanador]) : "",
                align: TextAlignment.RIGHT, bgOverride: bgGanador, fgOverride: fgGanador));

            doc.Add(proveedor);

            // ── FIRMAS ──────────────────────────────────────────────────────
            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();
            var firmantes = new (string Titulo, string Nombre)[]
            {
                ("SOLICITÁ",
                 "MARIA GABRIELA OLIVARES ROBLES\nJEFA DEL DEPARTAMENTO DE RECURSOS\nMATERIALES Y SERVICIOS GENERALES"),
                ("AUTORIZÓ",
                 "C. MARCOS MATAMOROS MORENO\nDIRECTOR DE ADMINISTRACIÓN Y FINANZAS"),
                ("V.BO",
                 "C. CIRO MIGUEL JUÁREZ PALACIOS\nTITULAR DE LA UNIDAD DE PLANEACIÓN,\nADMINISTRACIÓN Y FINANZAS"),
            };

            foreach (var (titulo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell()
                    .SetMinHeight(78f)
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPaddingTop(10f).SetPaddingBottom(10f)
                    .SetPaddingLeft(6f).SetPaddingRight(6f)
                    .SetBorder(borde)
                    .Add(new Paragraph(titulo)
                        .SetFont(fontBold).SetFontSize(5.75f)
                        .SetFontColor(textoEncabezado).SetMarginBottom(10f))
                    .Add(new Paragraph(" ").SetFontSize(22f))
                    .Add(new Paragraph("_________________________________________")
                        .SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoPrincipal).SetMarginBottom(6f))
                    .Add(new Paragraph(nombre.Replace("\n", " "))
                        .SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoSecundario)
                        .SetTextAlignment(TextAlignment.CENTER)));
            }
            doc.Add(tblFirmas);

            doc.Close();
            return ms.ToArray();
        }

        // ── DTOs internos ───────────────────────────────────────────────────
        private sealed class CuadroComparativoFilaPdf
        {
            public string Partida { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public decimal Cantidad { get; set; }       // numérico para cálculos
            public string CantidadTxt { get; set; } = ""; // texto formateado para mostrar
            public string Unidad { get; set; } = "";
            public decimal? PrecioP1 { get; set; }
            public decimal? PrecioP2 { get; set; }
            public decimal? PrecioP3 { get; set; }
            public bool IvaP1 { get; set; }
            public bool IvaP2 { get; set; }
            public bool IvaP3 { get; set; }
        }
    }
}