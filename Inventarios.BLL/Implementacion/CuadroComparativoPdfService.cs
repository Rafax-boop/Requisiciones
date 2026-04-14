using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace Inventario.BLL.Implementacion
{
    public class CuadroComparativoPdfService : ICuadroComparativoPdfService
    {
        private readonly IRequisicionesService _requisicionesService;

        public CuadroComparativoPdfService(IRequisicionesService requisicionesService)
        {
            _requisicionesService = requisicionesService;
        }

        public async Task<(byte[] PdfBytes, string FileName)?> GenerarAsync(
            int idRequisicion,
            string webRootPath,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(idRequisicion);
            if (dto == null)
                return null;

            var filas = dto.Articulos?
                .Select(a =>
                {
                    var cantidadTexto = a.Cantidad.HasValue
                        ? decimal.Truncate(a.Cantidad.Value) == a.Cantidad.Value
                            ? ((int)a.Cantidad.Value).ToString(CultureInfo.InvariantCulture)
                            : a.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture)
                        : "";

                    return new CuadroComparativoFilaPdf
                    {
                        Partida = a.NumPartida?.ToString() ?? "",
                        Descripcion = a.Descripcion ?? a.DescripcionDetallada ?? "",
                        Cantidad = cantidadTexto,
                        Unidad = a.UnidadMedida ?? ""
                    };
                })
                .ToList() ?? new List<CuadroComparativoFilaPdf>();

            var bytes = GenerarCuadroComparativoPdf(
                webRootPath,
                requisicion: dto.NumRequisicion ?? $"REQ-{idRequisicion}",
                fecha: dto.FechaEmision?.ToDateTime(TimeOnly.MinValue),
                departamento: dto.Departamento ?? "",
                justificacion: dto.Justificacion ?? "",
                filas: filas);

            var nombreArchivo =
                $"CuadroComparativo_{(dto.NumRequisicion ?? idRequisicion.ToString()).Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return (bytes, nombreArchivo);
        }

        private static byte[] GenerarCuadroComparativoPdf(
            string webRootPath,
            string requisicion,
            DateTime? fecha,
            string departamento,
            string justificacion,
            List<CuadroComparativoFilaPdf> filas)
        {
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
            var borde = new SolidBorder(new DeviceRgb(226, 232, 240), 0.85f);

            Cell CellHead(string text, int colspan = 1, int rowspan = 1, TextAlignment align = TextAlignment.LEFT)
                => new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetBackgroundColor(fondoEncabezado)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text).SetFont(fontBold).SetFontSize(6.6f).SetFontColor(textoEncabezado));

            Cell CellBody(string text, int colspan = 1, int rowspan = 1, TextAlignment align = TextAlignment.LEFT, float minHeight = 0f)
            {
                var c = new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text).SetFont(fontRegular).SetFontSize(6.8f).SetFontColor(textoPrincipal));
                if (minHeight > 0f)
                    c.SetMinHeight(minHeight);
                return c;
            }

            var header = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 64f, 18f })).UseAllAvailableWidth();
            header.SetBorder(borde);

            var logoIzq = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoIzq = System.IO.Path.Combine(webRootPath, "img", "corazon.png");
            if (File.Exists(rutaLogoIzq))
            {
                var img = new Image(ImageDataFactory.Create(rutaLogoIzq)).ScaleToFit(90f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                logoIzq.Add(img);
            }
            else
            {
                logoIzq.Add(new Paragraph("PUEBLA").SetFont(fontBold).SetFontSize(10f).SetTextAlignment(TextAlignment.CENTER));
            }
            header.AddCell(logoIzq);

            var centro = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetPadding(6f);
            centro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA DEL ESTADO DE PUEBLA")
                .SetFont(fontBold).SetFontSize(7.2f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DIRECCION DE ADMINISTRACION Y FINANZAS")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("CUADRO COMPARATIVO")
                .SetFont(fontBold).SetFontSize(10f).SetFontColor(rosaAcento).SetMarginTop(4f));
            header.AddCell(centro);

            var logoDer = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoDer = System.IO.Path.Combine(webRootPath, "img", "familias-dif-rosa.png");
            if (File.Exists(rutaLogoDer))
            {
                var img = new Image(ImageDataFactory.Create(rutaLogoDer)).ScaleToFit(95f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                logoDer.Add(img);
            }
            else
            {
                logoDer.Add(new Paragraph("Familias").SetFont(fontBold).SetFontSize(10f).SetTextAlignment(TextAlignment.CENTER));
            }
            header.AddCell(logoDer);
            doc.Add(header);
            doc.Add(new Paragraph(" ").SetMargin(2f));

            var fechaTxt = fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
            var info = new Table(UnitValue.CreatePercentArray(new float[] { 20f, 58f, 10f, 12f })).UseAllAvailableWidth();
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

            var tabla = new Table(UnitValue.CreatePercentArray(new float[] { 7f, 31f, 7f, 7f, 8f, 8f, 8f, 8f, 8f, 8f })).UseAllAvailableWidth();
            tabla.AddHeaderCell(CellHead("PARTIDA", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("DESCRIPCION", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("CANTIDAD", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("U.M.", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 1", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 2", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 3", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));

            var totalFilas = Math.Max(2, filas.Count);
            for (var i = 0; i < totalFilas; i++)
            {
                var fila = i < filas.Count ? filas[i] : new CuadroComparativoFilaPdf();
                var fondoFila = i % 2 == 1 ? fondoEncabezado : ColorConstants.WHITE;
                var c1 = CellBody(string.IsNullOrWhiteSpace(fila.Partida) ? (i + 1).ToString() : fila.Partida, align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c2 = CellBody(fila.Descripcion ?? "", minHeight: 17f).SetBackgroundColor(fondoFila);
                var c3 = CellBody(fila.Cantidad ?? "", align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c4 = CellBody(fila.Unidad ?? "", align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c5 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c6 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c7 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c8 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c9 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c10 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);

                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
                tabla.AddCell(c5);
                tabla.AddCell(c6);
                tabla.AddCell(c7);
                tabla.AddCell(c8);
                tabla.AddCell(c9);
                tabla.AddCell(c10);
            }

            foreach (var etiqueta in new[] { "SUMA", "IVA", "TOTAL" })
            {
                tabla.AddCell(CellBody("", 4));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
            }

            tabla.AddCell(CellHead("CONDICIONES DE PAGO", 4, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            doc.Add(tabla);
            doc.Add(new Paragraph(" ").SetMargin(3f));

            var proveedor = new Table(UnitValue.CreatePercentArray(new float[] { 44f, 16f, 10f, 10f, 10f, 10f })).UseAllAvailableWidth();
            proveedor.AddCell(CellHead("PROVEEDOR(ES) SELECCIONADO(S)"));
            proveedor.AddCell(CellBody(""));
            proveedor.AddCell(CellHead("SUMA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("IVA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("SUBTOTAL", align: TextAlignment.CENTER));
            proveedor.AddCell(CellBody("$", align: TextAlignment.CENTER));
            doc.Add(proveedor);
            doc.Add(new Paragraph(" ").SetMargin(8f));

            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();
            var firmantes = new (string Titulo, string Nombre)[]
            {
                ("ASIGNACIÓN PRESUPUESTAL", "JOEL MARTÍNEZ PÉREZ\nJEFE DEL DEPARTAMENTO DE RECURSOS\nFINANCIEROS"),
                ("Vo.Bo.", "C. MARCOS MATAMOROS MORENO\nDIRECTOR DE ADMINISTRACIÓN Y FINANZAS"),
                ("AUTORIZÓ", "C. CIRO MIGUEL JUÁREZ PALACIOS\nTITULAR DE LA UNIDAD DE PLANEACIÓN,\nADMINISTRACIÓN Y FINANZAS"),
            };

            foreach (var (titulo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell()
                    .SetMinHeight(78f)
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPaddingTop(10f)
                    .SetPaddingBottom(10f)
                    .SetPaddingLeft(6f)
                    .SetPaddingRight(6f)
                    .SetBorder(borde)
                    .Add(new Paragraph(titulo).SetFont(fontBold).SetFontSize(5.75f)
                        .SetFontColor(textoEncabezado).SetMarginBottom(10f))
                    .Add(new Paragraph(" ").SetFontSize(22f))
                    .Add(new Paragraph("_________________________________________").SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoPrincipal).SetMarginBottom(6f))
                    .Add(new Paragraph(nombre.Replace("\n", " ")).SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoSecundario)
                        .SetTextAlignment(TextAlignment.CENTER)));
            }
            doc.Add(tblFirmas);

            doc.Close();
            return ms.ToArray();
        }

        private sealed class CuadroComparativoFilaPdf
        {
            public string Partida { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Cantidad { get; set; } = "";
            public string Unidad { get; set; } = "";
        }
    }
}
