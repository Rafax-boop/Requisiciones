using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
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
using Microsoft.EntityFrameworkCore;
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
        private readonly IGenericRepository<TblAdjudicacion> _repositoryAdquisicion;
        private readonly IGenericRepository<TblConsolidada> _repositoryConsolidada;
        private readonly IGenericRepository<TblConsolidadasDetalle> _repositoryConsolidadaDetalle;
        private readonly IGenericRepository<TblRequisicionDetalle> _repositoryRequisicionDetalle;

        public CuadroComparativoPdfService(
            IRequisicionesService requisicionesService,
            IProveedoresService cotizacionesService,
            IGenericRepository<TblAdjudicacion> repositoryAdquisicion,
            IGenericRepository<TblConsolidada> repositoryConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repositoryConsolidadaDetalle,
            IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle)
        {
            _requisicionesService = requisicionesService;
            _cotizacionesService = cotizacionesService;
            _repositoryAdquisicion = repositoryAdquisicion;
            _repositoryConsolidada = repositoryConsolidada;
            _repositoryConsolidadaDetalle = repositoryConsolidadaDetalle;
            _repositoryRequisicionDetalle = repositoryRequisicionDetalle;
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

            // ── NUEVOS: tipo adjudicación y criterio ───────────────────────
            string tipoProcedimiento = "PENDIENTE DE CAPTURA";
            string criterioAdjudicacion = "SELECCION AL PROVEEDOR QUE CUMPLA CON REQUISITOS LEGALES Y OFERTE EL PRECIO MAS BAJO.";

            if (dto.IdAdquisicion.HasValue)
            {
                var tipoAdq = await _repositoryAdquisicion.Obtener(a => a.Id == dto.IdAdquisicion.Value);
                if (tipoAdq != null && !string.IsNullOrWhiteSpace(tipoAdq.Tipo))
                    tipoProcedimiento = tipoAdq.Tipo.ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(ganador?.Justificacion))
                criterioAdjudicacion = ganador.Justificacion.ToUpperInvariant();

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
                indiceGanadorForzado: indiceGanadorForzado,
                tipoProcedimiento: tipoProcedimiento,
                criterioAdjudicacion: criterioAdjudicacion);

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
            int indiceGanadorForzado = -1,
            string tipoProcedimiento = "PENDIENTE DE CAPTURA",
            string criterioAdjudicacion = "SELECCION AL PROVEEDOR...")
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
            info.AddCell(CellBody(tipoProcedimiento, 3));
            info.AddCell(CellHead("CRITERIO DE ADJUDICACION:"));
            info.AddCell(CellBody(criterioAdjudicacion, 3));
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

        public async Task<(byte[] PdfBytes, string FileName)?> GenerarReqAsync(
            int idRequisicion,
            string webRootPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(idRequisicion);
            if (dto == null) return null;

            // Obtener artículos para compra
            var articulos = await _cotizacionesService.ObtenerArticulosParaCompra(idRequisicion);

            // Obtener cotizaciones del proveedor ganador
            var ganador = await _cotizacionesService.ObtenerProveedorGanador(idRequisicion);
            var cotizaciones = await _cotizacionesService.ObtenerCotizaciones(idRequisicion);

            // Filtrar cotizaciones solo del ganador
            var cotizacionesGanador = cotizaciones
                .Where(c => ganador != null && c.IdProveedor == ganador.IdProveedor)
                .ToDictionary(c => c.IdPartida ?? 0, c => c.Importe);

            // Construir filas
            var filas = articulos.Select((a, idx) =>
            {
                var precioUnitario = cotizacionesGanador.TryGetValue(a.IdRequisicionDetalle, out var p) ? p : 0m;
                var cantidad = a.Cantidad ?? 0m;

                var cantidadTxt = a.Cantidad.HasValue
                    ? (decimal.Truncate(a.Cantidad.Value) == a.Cantidad.Value
                        ? ((int)a.Cantidad.Value).ToString(CultureInfo.InvariantCulture)
                        : a.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture))
                    : "";

                return new ReqDirectaFila
                {
                    NumPartida = idx + 1,
                    Descripcion = a.DescripcionDetallada ?? a.Descripcion ?? "",
                    Cantidad = cantidad,
                    CantidadTxt = cantidadTxt,
                    UnidadMedida = a.UnidadMedida ?? "",
                    PrecioUnitario = precioUnitario,
                    Total = precioUnitario * cantidad
                };
            }).ToList();

            var fecha = dto.FechaEmision?.ToDateTime(TimeOnly.MinValue);
            var bytes = GenerarPdf(
                webRootPath,
                requisicion: dto.NumRequisicion ?? $"REQ-{idRequisicion}",
                fecha: fecha,
                areasSolicitantes: dto.Departamento ?? "",
                filas: filas);

            var nombreArchivo =
                $"ReqDirecta_{(dto.NumRequisicion ?? idRequisicion.ToString()).Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (bytes, nombreArchivo);
        }

        private static byte[] GenerarPdf(
            string webRootPath,
            string requisicion,
            DateTime? fecha,
            string areasSolicitantes,
            List<ReqDirectaFila> filas)
        {
            const decimal tasaIva = 0.16m;

            var suma = filas.Sum(f => f.Total);
            var iva = filas.Sum(f => f.Total * tasaIva);
            var total = suma + iva;

            static string Moneda(decimal v) =>
                v == 0 ? "$             -" : v.ToString("C2", new CultureInfo("es-MX"));

            static string MonedaFmt(decimal v) =>
                v == 0 ? "$             -" : v.ToString("C2", new CultureInfo("es-MX"));

            var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            // Portrait A4 (como la imagen)
            using var doc = new Document(pdf, PageSize.A4);
            doc.SetMargins(18f, 18f, 18f, 18f);

            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var fondoEncabezado = new DeviceRgb(248, 250, 252);
            var textoPrincipal = new DeviceRgb(26, 26, 26);
            var textoSecundario = new DeviceRgb(100, 116, 139);
            var textoEncabezado = new DeviceRgb(71, 85, 105);
            var textoInstitucional = new DeviceRgb(45, 45, 45);
            var rosaAcento = new DeviceRgb(255, 45, 111);
            var borde = new SolidBorder(new DeviceRgb(180, 180, 180), 0.7f);

            // ── helpers de celda ──────────────────────────────────────────
            Cell CellHead(string text, int colspan = 1, int rowspan = 1,
                TextAlignment align = TextAlignment.LEFT,
                DeviceRgb? bgOverride = null, DeviceRgb? fgOverride = null)
                => new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetBackgroundColor(bgOverride ?? fondoEncabezado)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text)
                        .SetFont(fontBold)
                        .SetFontSize(7f)
                        .SetFontColor(fgOverride ?? textoEncabezado));

            Cell CellBody(string text, int colspan = 1, int rowspan = 1,
                TextAlignment align = TextAlignment.LEFT, float minHeight = 0f,
                DeviceRgb? bgOverride = null, DeviceRgb? fgOverride = null)
            {
                var c = new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text)
                        .SetFont(fontRegular)
                        .SetFontSize(7.2f)
                        .SetFontColor(fgOverride ?? textoPrincipal));
                if (bgOverride != null) c.SetBackgroundColor(bgOverride);
                if (minHeight > 0f) c.SetMinHeight(minHeight);
                return c;
            }

            // ── ENCABEZADO ────────────────────────────────────────────────
            var header = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 64f, 18f }))
                .UseAllAvailableWidth();
            header.SetBorder(borde);

            // Logo izquierdo
            var logoIzq = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoIzq = System.IO.Path.Combine(webRootPath, "img", "corazon.png");
            if (File.Exists(rutaLogoIzq))
                logoIzq.Add(new Image(ImageDataFactory.Create(rutaLogoIzq))
                    .ScaleToFit(90f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                logoIzq.Add(new Paragraph("PUEBLA").SetFont(fontBold).SetFontSize(10f));
            header.AddCell(logoIzq);

            // Centro
            var centro = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(6f);
            centro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA DEL ESTADO DE PUEBLA")
                .SetFont(fontBold).SetFontSize(7.2f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DIRECCIÓN DE ADMINISTRACIÓN Y FINANZAS")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            header.AddCell(centro);

            // Logo derecho — celda de fecha integrada en el header
            var logoDerContainer = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(4f);
            var rutaLogoDer = System.IO.Path.Combine(webRootPath, "img", "familias-dif-rosa.png");
            if (File.Exists(rutaLogoDer))
                logoDerContainer.Add(new Image(ImageDataFactory.Create(rutaLogoDer))
                    .ScaleToFit(95f, 38f).SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                logoDerContainer.Add(new Paragraph("Familias").SetFont(fontBold).SetFontSize(10f));
            header.AddCell(logoDerContainer);
            doc.Add(header);

            // ── FECHA (fila separada alineada a la derecha) ───────────────
            var fechaTxt = fecha.HasValue
                ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)
                : "";
            var tablaFecha = new Table(UnitValue.CreatePercentArray(new float[] { 82f, 18f }))
                .UseAllAvailableWidth();
            tablaFecha.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            var celdaFecha = new Cell().SetBorder(borde).SetPadding(4f)
                .SetTextAlignment(TextAlignment.CENTER);
            celdaFecha.Add(new Paragraph("FECHA:")
                .SetFont(fontBold).SetFontSize(7f).SetFontColor(textoEncabezado));
            celdaFecha.Add(new Paragraph(fechaTxt)
                .SetFont(fontRegular).SetFontSize(7.5f).SetFontColor(textoPrincipal));
            tablaFecha.AddCell(celdaFecha);
            doc.Add(tablaFecha);

            doc.Add(new Paragraph(" ").SetMargin(2f));

            // ── SECCIÓN DE ADQUISICIONES ──────────────────────────────────
            var tablaInfo = new Table(UnitValue.CreatePercentArray(new float[] { 30f, 70f }))
                .UseAllAvailableWidth();

            tablaInfo.AddCell(CellHead("SECCIÓN DE ADQUISICIONES", 2, 1, TextAlignment.LEFT));

            tablaInfo.AddCell(CellHead("TIPO DE PROCEDIMIENTO:"));
            tablaInfo.AddCell(CellBody(
                "COMPRA DIRECTA A TRAVÉS DE LAS DEPENDENCIAS Y ENTIDADES CONFORME AL ART. 47 APARTADO H DE LA LEY DE EGRESOS"));

            tablaInfo.AddCell(CellHead("No. REQUISICIÓN:"));
            tablaInfo.AddCell(CellBody(requisicion));

            tablaInfo.AddCell(CellHead("AREA SOLICITANTE:"));
            tablaInfo.AddCell(CellBody(string.IsNullOrWhiteSpace(areasSolicitantes) ? " " : areasSolicitantes));

            doc.Add(tablaInfo);
            doc.Add(new Paragraph(" ").SetMargin(3f));

            // ── TABLA DE ARTÍCULOS ────────────────────────────────────────
            // Columnas: PARTIDA | DESCRIPCION | CANTIDAD | U.M. | P/UNITARIO | TOTAL
            var tabla = new Table(UnitValue.CreatePercentArray(
                new float[] { 8f, 42f, 10f, 10f, 15f, 15f }))
                .UseAllAvailableWidth();

            tabla.AddHeaderCell(CellHead("PARTIDA", align: TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("DESCRIPCIÓN", align: TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("CANTIDAD", align: TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("U.M.", align: TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", align: TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", align: TextAlignment.CENTER));

            // Mínimo 15 filas visibles (igual a la imagen)
            const int MIN_FILAS = 15;
            var totalFilas = Math.Max(filas.Count, MIN_FILAS);

            for (var i = 0; i < totalFilas; i++)
            {
                var fondoFila = i % 2 == 1 ? fondoEncabezado : ColorConstants.WHITE;
                if (i < filas.Count)
                {
                    var fila = filas[i];
                    tabla.AddCell(CellBody(fila.NumPartida.ToString(),
                        align: TextAlignment.CENTER, minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody(fila.Descripcion,
                        minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody(fila.CantidadTxt,
                        align: TextAlignment.CENTER, minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody(fila.UnidadMedida,
                        align: TextAlignment.CENTER, minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody(MonedaFmt(fila.PrecioUnitario),
                        align: TextAlignment.RIGHT, minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody(MonedaFmt(fila.Total),
                        align: TextAlignment.RIGHT, minHeight: 16f).SetBackgroundColor(fondoFila));
                }
                else
                {
                    // Fila vacía
                    for (var col = 0; col < 4; col++)
                        tabla.AddCell(CellBody("", minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody("$             -",
                        align: TextAlignment.RIGHT, minHeight: 16f).SetBackgroundColor(fondoFila));
                    tabla.AddCell(CellBody("$             -",
                        align: TextAlignment.RIGHT, minHeight: 16f).SetBackgroundColor(fondoFila));
                }
            }

            // ── Filas SUMA / IVA / TOTAL ──────────────────────────────────
            var resumen = new[] {
                ("SUMA",  suma),
                ("IVA",   iva),
                ("TOTAL", total)
            };

            foreach (var (etiqueta, valor) in resumen)
            {
                tabla.AddCell(new Cell(1, 4).SetBorder(Border.NO_BORDER));
                tabla.AddCell(CellHead(etiqueta, align: TextAlignment.CENTER));
                var textoValor = (etiqueta == "IVA" && valor == 0) ? "—" : Moneda(valor);
                tabla.AddCell(CellBody(textoValor, align: TextAlignment.RIGHT));
            }

            // ── Condiciones de pago ───────────────────────────────────────
            tabla.AddCell(CellHead("CONDICIONES DE PAGO", 4, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));

            doc.Add(tabla);
            doc.Add(new Paragraph(" ").SetMargin(4f));

            // ── FIRMAS ────────────────────────────────────────────────────
            var tblFirmas = new Table(UnitValue.CreatePercentArray(new float[] { 33.3f, 33.3f, 33.4f }))
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
                    .SetMinHeight(80f)
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPaddingTop(10f).SetPaddingBottom(10f)
                    .SetPaddingLeft(6f).SetPaddingRight(6f)
                    .SetBorder(borde)
                    .Add(new Paragraph(titulo)
                        .SetFont(fontBold).SetFontSize(6f)
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

        public async Task<(byte[] PdfBytes, string FileName)?> GenerarConsolidadoAsync(
    int idConsolidada,
    string webRootPath,
    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 1. Obtener la consolidada
            var consolidada = await _repositoryConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null) return null;

            // 2. Obtener ids de requisiciones hijas
            var qDetalles = await _repositoryConsolidadaDetalle
                .Consultar(d => d.ConsolidadaId == idConsolidada);
            var detallesConsolidada = await qDetalles.ToListAsync();
            var idsRequisiciones = detallesConsolidada.Select(d => d.IdRequisicion).ToList();

            if (!idsRequisiciones.Any()) return null;

            // 3. Traer TODOS los artículos de todas las hijas
            var qArticulos = await _repositoryRequisicionDetalle
                .Consultar(d => idsRequisiciones.Contains(d.IdRequisicion));
            var todosLosArticulos = await qArticulos.ToListAsync();

            // 4. Agrupar por IdArticulo sumando cantidades
            var articulosAgrupados = todosLosArticulos
                .GroupBy(a => a.IdArticulo)
                .Select(g =>
                {
                    var primero = g.First();
                    return new DetalleArticuloDTO
                    {
                        IdRequisicionDetalle = primero.IdRequisicionDetalle, // referencia para cotizaciones
                        NumPartida = primero.NumPartida,
                        IdArticulo = primero.IdArticulo,
                        Cantidad = g.Sum(x => x.Cantidad),
                        UnidadMedida = primero.UnidadMedida,
                        Descripcion = primero.Descripcion,
                        DescripcionDetallada = primero.DescripcionDetallada
                    };
                })
                .ToList();

            var idReqReferencia = idsRequisiciones.First();
            var ganador = await _cotizacionesService.ObtenerProveedorGanador(idReqReferencia);
            var idProveedorGanado = ganador?.IdProveedor ?? 0;

            // 6. Tipo de procedimiento y criterio (igual que el individual)
            string tipoProcedimiento = "PENDIENTE DE CAPTURA";
            string criterioAdjudicacion = "SELECCION AL PROVEEDOR QUE CUMPLA CON REQUISITOS LEGALES Y OFERTE EL PRECIO MAS BAJO.";

            if (consolidada.IdAdjudicacion.HasValue)
            {
                var tipoAdq = await _repositoryAdquisicion
                    .Obtener(a => a.Id == consolidada.IdAdjudicacion.Value);
                if (tipoAdq != null && !string.IsNullOrWhiteSpace(tipoAdq.Tipo))
                    tipoProcedimiento = tipoAdq.Tipo.ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(ganador?.Justificacion))
                criterioAdjudicacion = ganador.Justificacion.ToUpperInvariant();

            // 7. Construir mapa cotizaciones por IdRequisicionDetalle del primero de cada grupo
            //    Necesitamos mapear: para cada artículo agrupado, buscar su precio
            //    en las cotizaciones de las hijas por IdArticulo

            // Mapa: idArticulo -> List<cotizacion> (buscando en todas las hijas)
            var todasLasCotizaciones = new List<CotizacionDTO>();
            foreach (var idReq in idsRequisiciones)
            {
                var cotsDeEstaReq = await _cotizacionesService.ObtenerCotizaciones(idReq);
                // Enriquecer con idArticulo usando el detalle
                var detallesDeEstaReq = todosLosArticulos
                    .Where(a => a.IdRequisicion == idReq).ToList();

                foreach (var cot in cotsDeEstaReq)
                {
                    // Buscar el IdArticulo de esta partida
                    var detalle = detallesDeEstaReq
                        .FirstOrDefault(d => d.IdRequisicionDetalle == cot.IdPartida);
                    if (detalle != null)
                    {
                        // Crear copia con IdPartida = IdArticulo para el agrupamiento
                        todasLasCotizaciones.Add(new CotizacionDTO
                        {
                            IdProveedor = cot.IdProveedor,
                            NombreProveedor = cot.NombreProveedor,
                            Importe = cot.Importe,
                            IVA = cot.IVA,
                            IdPartida = detalle.IdArticulo // ← clave: agrupamos por artículo
                        });
                    }
                }
            }

            // Rebuild cotsPorPartida usando idArticulo como clave
            var cotsPorPartidaConsolidada = todasLasCotizaciones
                .GroupBy(c => c.IdPartida) // IdPartida aquí = IdArticulo
                .ToDictionary(g => g.Key, g => g.ToList());

            // 8. Proveedores únicos
            var proveedores = todasLasCotizaciones
                .Select(c => new { c.IdProveedor, c.NombreProveedor })
                .DistinctBy(p => p.IdProveedor)
                .OrderBy(p => p.IdProveedor == idProveedorGanado ? 0 : 1)
                .Take(3)
                .ToList();

            var indiceGanadorForzado = idProveedorGanado > 0
                ? proveedores.FindIndex(p => p.IdProveedor == idProveedorGanado)
                : -1;

            // 9. Construir filas usando articulosAgrupados
            var filas = articulosAgrupados.Select(a =>
            {
                var cantidadTexto = a.Cantidad.HasValue
                    ? decimal.Truncate(a.Cantidad.Value) == a.Cantidad.Value
                        ? ((int)a.Cantidad.Value).ToString(CultureInfo.InvariantCulture)
                        : a.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture)
                    : "";

                var cotsDeLaPartida = cotsPorPartidaConsolidada
                    .TryGetValue(a.IdArticulo, out var lista)
                    ? lista : new List<CotizacionDTO>();

                var precios = proveedores
                    .Select(p => cotsDeLaPartida.FirstOrDefault(c => c.IdProveedor == p.IdProveedor))
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
            }).ToList();

            // 10. Datos de encabezado (tomados de la primera requisición hija)
            var primeraReq = await _requisicionesService
                .ObtenerRequisicionCompletaPorId(idReqReferencia);

            var nombresProveedores = proveedores.Select(p => p.NombreProveedor).ToArray();

            var bytes = GenerarCuadroComparativoPdf(
                webRootPath,
                requisicion: consolidada.FolioConsolidada,
                fecha: primeraReq?.FechaEmision?.ToDateTime(TimeOnly.MinValue),
                departamento: primeraReq?.Departamento ?? "",
                justificacion: primeraReq?.Justificacion ?? "",
                filas: filas,
                nombresProveedores: nombresProveedores,
                indiceGanadorForzado: indiceGanadorForzado,
                tipoProcedimiento: tipoProcedimiento,
                criterioAdjudicacion: criterioAdjudicacion);

            var nombreArchivo =
                $"CuadroComparativo_Cons_{consolidada.FolioConsolidada.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (bytes, nombreArchivo);
        }

        public async Task<(byte[] PdfBytes, string FileName)?> GenerarConsolidadoReqAsync(
    int idConsolidada,
    string webRootPath,
    CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var consolidada = await _repositoryConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null) return null;

            var qDetalles = await _repositoryConsolidadaDetalle
                .Consultar(d => d.ConsolidadaId == idConsolidada);
            var detallesConsolidada = await qDetalles.ToListAsync();
            var idsRequisiciones = detallesConsolidada.Select(d => d.IdRequisicion).ToList();
            if (!idsRequisiciones.Any()) return null;

            var qArticulos = await _repositoryRequisicionDetalle
                .Consultar(d => idsRequisiciones.Contains(d.IdRequisicion));
            var todosLosArticulos = await qArticulos.ToListAsync();

            var idReqReferencia = idsRequisiciones.First();
            var ganador = await _cotizacionesService.ObtenerProveedorGanador(idReqReferencia);

            var cotizacionesTodas = new List<(int IdArticulo, decimal Importe)>();
            foreach (var idReq in idsRequisiciones)
            {
                var cots = await _cotizacionesService.ObtenerCotizaciones(idReq);
                var detallesReq = todosLosArticulos.Where(a => a.IdRequisicion == idReq).ToList();
                foreach (var cot in cots.Where(c => ganador != null && c.IdProveedor == ganador.IdProveedor))
                {
                    var detalle = detallesReq.FirstOrDefault(d => d.IdRequisicionDetalle == cot.IdPartida);
                    if (detalle?.IdArticulo != null)
                        cotizacionesTodas.Add((detalle.IdArticulo.Value, cot.Importe));
                }
            }

            var precioPorArticulo = cotizacionesTodas
                .GroupBy(c => c.IdArticulo)
                .ToDictionary(g => g.Key, g => g.First().Importe);

            var articulosAgrupados = todosLosArticulos
                .GroupBy(a => a.IdArticulo)
                .Select((g, idx) =>
                {
                    var primero = g.First();
                    var cantidad = g.Sum(x => x.Cantidad) ?? 0m;
                    var cantidadTxt = decimal.Truncate(cantidad) == cantidad
                        ? ((int)cantidad).ToString(CultureInfo.InvariantCulture)
                        : cantidad.ToString("0.##", CultureInfo.InvariantCulture);
                    var precioUnitario = precioPorArticulo.TryGetValue(g.Key ?? 0, out var p) ? p : 0m;

                    return new ReqDirectaFila
                    {
                        NumPartida = idx + 1,
                        Descripcion = primero.DescripcionDetallada ?? primero.Descripcion ?? "",
                        Cantidad = cantidad,
                        CantidadTxt = cantidadTxt,
                        UnidadMedida = primero.UnidadMedida ?? "",
                        PrecioUnitario = precioUnitario,
                        Total = precioUnitario * cantidad
                    };
                })
                .ToList();

            var primeraReq = await _requisicionesService
                .ObtenerRequisicionCompletaPorId(idReqReferencia);
            var fecha = primeraReq?.FechaEmision?.ToDateTime(TimeOnly.MinValue);

            var bytes = GenerarPdf(
                webRootPath,
                requisicion: consolidada.FolioConsolidada,
                fecha: fecha,
                areasSolicitantes: primeraReq?.Departamento ?? "",
                filas: articulosAgrupados);

            var nombreArchivo =
                $"ReqDirecta_Cons_{consolidada.FolioConsolidada.Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (bytes, nombreArchivo);
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

        private sealed class ReqDirectaFila
        {
            public int NumPartida { get; set; }
            public string Descripcion { get; set; } = "";
            public decimal Cantidad { get; set; }
            public string CantidadTxt { get; set; } = "";
            public string UnidadMedida { get; set; } = "";
            public decimal PrecioUnitario { get; set; }
            public decimal Total { get; set; }
        }
    }
}