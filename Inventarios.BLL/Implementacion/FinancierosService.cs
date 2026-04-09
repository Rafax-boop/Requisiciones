using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class FinancierosService : IFinancierosService
    {
        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblBitacoraEstatus> _repositoryBitacora;
        private readonly IGenericRepository<TblRegistroDiseno> _repositoryDiseno;
        private readonly IGenericRepository<TblDepartamento> _repoDepartamento;
        private readonly IGenericRepository<TblRequisicionDetalle> _repoDetalle;

        public FinancierosService(IRequisicionRepository repositoryRequisicion, IGenericRepository<TblBitacoraEstatus> repositoryBitacora, IGenericRepository<TblRegistroDiseno> repositoryDiseno, IGenericRepository<TblDepartamento> repoDepartamento, IGenericRepository<TblRequisicionDetalle> repoDetalle)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDiseno = repositoryDiseno;
            _repoDepartamento = repoDepartamento;
            _repoDetalle = repoDetalle;
        }
        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idUsuarioFinancieros = null)
        {
            IQueryable<TblRequisicion> query;

            if (idUsuarioFinancieros.HasValue)
            {
                query = await _repositoryRequisicion.Consultar(r => r.IdUsuarioFinan == idUsuarioFinancieros.Value);
            }
            else
            {
                query = await _repositoryRequisicion.Consultar();
            }

            var resultado = await query
                .Where(r => r.IdUsuarioFinan.HasValue || r.IdEstatus == 13)
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequisicion)
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    FechaModificacion = r.FechaModificacion,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
                    IdEstatus = r.IdEstatus ?? 0,
                    Estatus = r.IdEstatusNavigation.NombreEstatus,
                    CantidadPartidas = r.TblRequisicionDetalles.Count,
                    DiasAsignado = r.TblBitacoraEstatuses
                        .Where(b => b.IdEstatus == 14)
                        .OrderByDescending(b => b.FechaEstatus)
                        .Select(b => (DateTime.Now - (b.FechaEstatus ?? DateTime.Now)).Days)
                        .FirstOrDefault(),
                    NombreAsignado = r.IdUsuarioFinanNavigation != null ? r.IdUsuarioFinanNavigation.Usuario : null,
                    RequiServicio = r.RequiServicio
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<bool> AsignarRequisicion(int idRequi, int idUsuario, int idUsuarioFinan)
        {
            try
            {
                var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequi);

                if (requisicion == null)
                    return false;


                requisicion.IdUsuarioFinan = idUsuarioFinan;
                requisicion.IdEstatus = 14;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = requisicion.IdEstatus,
                    FechaEstatus = DateTime.Now,
                    Observacion = "AsignarFinancieros",
                    IdUsuario = idUsuario
                };
                var bitacoraCreada = await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == modelo.IdRequisicion);
                if (requisicion == null) return false;

                // Actualizar estatus y número de API
                requisicion.IdEstatus = 15;
                requisicion.FechaModificacion = DateTime.Now;
                requisicion.NumApi = await GenerarNumeroApiAsync();

                await _repositoryRequisicion.Editar(requisicion);

                // Bitácora
                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = 15,
                    FechaEstatus = DateTime.Now,
                    Observacion = modelo.Observaciones,
                    IdUsuario = idUsuario
                };
                await _repositoryBitacora.Crear(bitacora);

                // Guardar archivos
                await GuardarArchivos(modelo.DocSiaf, modelo.IdRequisicion, "SIAF", "DocumentoSIAF");
                await GuardarArchivos(modelo.TablaApi, modelo.IdRequisicion, "TablaApi", "TablaApi");

                return true;
            }
            catch { throw; }
        }

        public async Task<bool> FinalizarRequisicion(int idRequisicion, List<IFormFile>? facturas, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == idRequisicion);

                if (requisicion == null) return false;

                requisicion.IdEstatus = 7;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = idRequisicion,
                    IdEstatus = 7,
                    FechaEstatus = DateTime.Now,
                    Observacion = "Pago finalizado",
                    IdUsuario = idUsuario
                };

                await _repositoryBitacora.Crear(bitacora);

                if (facturas != null && facturas.Any())
                {
                    var rutaBase = System.IO.Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "uploads", "Factura", idRequisicion.ToString());

                    Directory.CreateDirectory(rutaBase);

                    foreach (var archivo in facturas)
                    {
                        if (archivo.Length == 0) continue;

                        var nombreArchivo = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(archivo.FileName)}";
                        var rutaFisica = System.IO.Path.Combine(rutaBase, nombreArchivo);

                        using (var stream = new FileStream(rutaFisica, FileMode.Create))
                            await archivo.CopyToAsync(stream);

                        var registro = new TblRegistroDiseno
                        {
                            IdRequisicion = idRequisicion,
                            Ruta = $"/uploads/Factura/{idRequisicion}/{nombreArchivo}",
                            FechaSubida = DateTime.Now,
                            Tipo = "Factura"
                        };
                        await _repositoryDiseno.Crear(registro);
                    }
                }

                return true;
            }
            catch { throw; }
        }

        private async Task GuardarArchivos(
            List<IFormFile>? archivos,
            int idRequisicion,
            string tipo,
            string carpeta)
        {
            if (archivos == null || !archivos.Any()) return;

            // Ruta: wwwroot/uploads/DocumentoSIAF/{idRequisicion}/ o TablaApi/{idRequisicion}/
            var rutaBase = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", carpeta, idRequisicion.ToString());

            Directory.CreateDirectory(rutaBase);

            foreach (var archivo in archivos)
            {
                if (archivo.Length == 0) continue;

                var nombreArchivo = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(archivo.FileName)}";
                var rutaFisica = System.IO.Path.Combine(rutaBase, nombreArchivo);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    await archivo.CopyToAsync(stream);

                // Ruta relativa para guardar en BD
                var rutaBd = $"/uploads/{carpeta}/{idRequisicion}/{nombreArchivo}";

                var registro = new TblRegistroDiseno
                {
                    IdRequisicion = idRequisicion,
                    Ruta = rutaBd,
                    FechaSubida = DateTime.Now,
                    Tipo = tipo
                };

                await _repositoryDiseno.Crear(registro);
            }
        }

        public async Task<byte[]> GenerarTablaApiAsync(int idRequisicion)
        {
            // ── 1. Obtener datos ─────────────────────────────────────────────
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion)
                ?? throw new Exception($"No se encontró la requisición {idRequisicion}.");

            string nombreDepartamento = "";
            if (requisicion.IdDepartamento.HasValue)
            {
                var depto = await _repoDepartamento
                    .Obtener(d => d.IdDepartamento == requisicion.IdDepartamento.Value);
                nombreDepartamento = depto?.NombreDepartamento ?? "";
            }

            var queryDet = await _repoDetalle.Consultar(d => d.IdRequisicion == idRequisicion);
            var detalles = await queryDet.ToListAsync();

            // ── 2. Generar PDF ───────────────────────────────────────────────
            // IMPORTANTE: NO usar "using" en el MemoryStream.
            // iText cierra el stream al hacer doc.Close(), por eso hay que
            // leer los bytes DESPUÉS del Close() con LeaveOpen=false (default).
            // La solución correcta es no hacer Dispose() antes de leer ToArray().
            var ms = new MemoryStream();

            // LeaveOpen = false (default) — iText cerrará el writer pero NO el stream
            // cuando usamos el constructor que acepta el MemoryStream directamente.
            var pdfWriter = new PdfWriter(ms);

            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, PageSize.LETTER);
            doc.SetMargins(10f, 10f, 10f, 10f);

            // Fuentes estándar embebidas — no requieren archivos en disco
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var gris = new DeviceGray(0.82f);
            var negro = ColorConstants.BLACK;

            // ── Helpers locales ──────────────────────────────────────────────
            Cell CeldaGris(string texto, int colspan = 1, int rowspan = 1, float size = 6f,
                           TextAlignment align = TextAlignment.CENTER)
            {
                var p = new Paragraph(S(texto)).SetFont(bold).SetFontSize(size);
                return new Cell(rowspan, colspan)
                    .SetBackgroundColor(gris)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(2f)
                    .Add(p);
            }

            Cell CeldaBlanca(string texto, float size = 6f, bool negrita = false,
                             TextAlignment align = TextAlignment.CENTER)
            {
                var p = new Paragraph(S(texto)).SetFont(negrita ? bold : regular).SetFontSize(size);
                return new Cell()
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(2f)
                    .Add(p);
            }

            Cell CeldaVacia(float height = 14f) =>
                new Cell().SetHeight(height).SetPadding(0f);

            // ── Fecha — FechaEmision es DateOnly? en tu entidad ──────────────
            string fechaStr;
            try
            {
                if (requisicion.FechaEmision.HasValue)
                {
                    // DateOnly → DateTime para poder formatear con cultura
                    var dt = requisicion.FechaEmision.Value.ToDateTime(TimeOnly.MinValue);
                    fechaStr = dt.ToString("dddd, d 'de' MMMM 'de' yyyy",
                        new System.Globalization.CultureInfo("es-MX"));
                }
                else
                {
                    fechaStr = DateTime.Now.ToString("dddd, d 'de' MMMM 'de' yyyy",
                        new System.Globalization.CultureInfo("es-MX"));
                }
            }
            catch
            {
                fechaStr = DateTime.Now.ToShortDateString();
            }

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 1 — ENCABEZADO
            // ════════════════════════════════════════════════════════════════
            var tblEnc = new Table(UnitValue.CreatePointArray(new float[] { 90f, 320f, 110f }))
                .UseAllAvailableWidth();

            // Celda PUEBLA (rowspan 5)
            tblEnc.AddCell(new Cell(5, 1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .Add(new Paragraph("PUEBLA").SetFont(bold).SetFontSize(11))
                .Add(new Paragraph("Gobierno del Estado").SetFont(regular).SetFontSize(6))
                .Add(new Paragraph("2 0 2 4 - 2 0 3 0").SetFont(regular).SetFontSize(5)));

            // Celda título central (1 fila)
            tblEnc.AddCell(new Cell()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(3f)
                .Add(new Paragraph("Autorización Presupuestal Interna").SetFont(bold).SetFontSize(11))
                .Add(new Paragraph("Dirección de Administración y Finanzas").SetFont(regular).SetFontSize(8))
                .Add(new Paragraph("Departamento de Recursos Materiales y Servicios Generales").SetFont(regular).SetFontSize(7))
                .Add(new Paragraph("Departamento de Recursos Financieros").SetFont(bold).SetFontSize(7))
                .Add(new Paragraph("EJERCICIO 2025").SetFont(bold).SetFontSize(8)));

            // Celda Familias DIF (rowspan 5)
            tblEnc.AddCell(new Cell(5, 1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .Add(new Paragraph("Familias").SetFont(bold).SetFontSize(12))
                .Add(new Paragraph("Sistema Estatal DIF").SetFont(regular).SetFontSize(7))
                .Add(new Paragraph(" ").SetFontSize(4f))
                .Add(new Paragraph("Fecha de elaboración:").SetFont(bold).SetFontSize(6))
                .Add(new Paragraph(fechaStr).SetFont(regular).SetFontSize(5.5f)));

            // 4 celdas vacías para completar el rowspan de PUEBLA y Familias
            for (int i = 0; i < 4; i++)
                tblEnc.AddCell(new Cell().SetHeight(10f).SetPadding(0f));

            doc.Add(tblEnc);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 2 — ÁREA SOLICITANTE
            // ════════════════════════════════════════════════════════════════
            var tblArea = new Table(UnitValue.CreatePointArray(new float[] { 85f, 38f, 397f }))
                .UseAllAvailableWidth();

            tblArea.AddCell(CeldaGris("ÁREA SOLICITANTE:", align: TextAlignment.LEFT));
            tblArea.AddCell(CeldaBlanca(S(requisicion.IdDepartamento?.ToString()), negrita: true));
            tblArea.AddCell(CeldaBlanca(nombreDepartamento, align: TextAlignment.LEFT));

            foreach (var lbl in new[] { "ADECUACIÓN:", "AUTORIZACIÓN:", "SOLICITUD DE PAGO:" })
            {
                tblArea.AddCell(CeldaGris(lbl, align: TextAlignment.LEFT));
                tblArea.AddCell(new Cell(1, 2).SetHeight(10f).SetPadding(0f));
            }

            doc.Add(tblArea);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 3 — INFORMACIÓN DEL BIEN O SERVICIO
            // ════════════════════════════════════════════════════════════════
            var tblBien = new Table(UnitValue.CreatePointArray(new float[] { 105f, 415f }))
                .UseAllAvailableWidth();

            tblBien.AddCell(new Cell(1, 2)
                .SetBackgroundColor(gris)
                .SetFont(bold).SetFontSize(6.5f)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(2f)
                .Add(new Paragraph("INFORMACIÓN DEL BIEN O SERVICIO POR ADQUIRIR")));

            tblBien.AddCell(CeldaGris("DESCRIPCIÓN DETALLADA\nDEL BIEN O SERVICIO:").SetHeight(28f));
            tblBien.AddCell(new Cell()
                .SetFont(regular).SetFontSize(7f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(3f)
                .Add(new Paragraph(S(requisicion.UsoEspecifico))));

            tblBien.AddCell(CeldaGris("JUSTIFICACIÓN:").SetHeight(28f));
            tblBien.AddCell(new Cell()
                .SetFont(regular).SetFontSize(6.5f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(3f)
                .Add(new Paragraph(S(requisicion.Justificacion))));

            doc.Add(tblBien);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 4 — TABLA DE PARTIDAS
            // ════════════════════════════════════════════════════════════════
            // Anchos de columna: No | UA | ClaveMun | ImpSol | FuenteFin | PP | Comp | Act | ObjGasto | ImpAut
            var colWidths = new float[] { 20f, 26f, 36f, 52f, 50f, 30f, 42f, 36f, 40f, 50f };
            var tblPart = new Table(UnitValue.CreatePointArray(colWidths)).UseAllAvailableWidth();

            // Sub-encabezados de sección (fila 1 del header)
            tblPart.AddHeaderCell(new Cell(1, 4)
                .SetBackgroundColor(gris).SetFont(bold).SetFontSize(5.5f)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(2f)
                .Add(new Paragraph("SOLICITUD DE SUFICIENCIA")));

            tblPart.AddHeaderCell(new Cell(1, 6)
                .SetBackgroundColor(gris).SetFont(bold).SetFontSize(5.5f)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(2f)
                .Add(new Paragraph("AUTORIZACIÓN LA SECCIÓN DE PROGRAMACIÓN PRESUPUESTAL Y FINANCIERA")));

            // Encabezados de columna (fila 2 del header)
            var colTitles = new[]
            {
                "No.", "UA", "CLAVE\nMUNICIPIO", "IMPORTE\nSOLICITADO",
                "FUENTE DE\nFINANCIAMIENTO", "PP", "COMPONENTE",
                "ACTIVIDAD", "OBJETO\nDEL GASTO", "IMPORTE\nAUTORIZADO"
            };
            foreach (var h in colTitles)
                tblPart.AddHeaderCell(CeldaGris(h, size: 5f).SetHeight(18f));

            // Filas de datos
            int numFilas = Math.Max(15, detalles.Count + 3);
            for (int i = 0; i < numFilas; i++)
            {
                if (i < detalles.Count)
                {
                    var d = detalles[i];
                    tblPart.AddCell(CeldaBlanca((i + 1).ToString()));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.IdDepartamento?.ToString())));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.ClaveRegion?.ToString())));
                    tblPart.AddCell(CeldaVacia());   // Importe Solicitado — financiero lo llena
                    tblPart.AddCell(CeldaBlanca(S(requisicion.Ff)));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.IdPp?.ToString())));
                    tblPart.AddCell(CeldaVacia());   // Componente — financiero lo llena
                    tblPart.AddCell(CeldaVacia());   // Actividad — financiero lo llena
                    tblPart.AddCell(CeldaBlanca(S(d.CogEditable?.ToString() ?? d.NumPartida?.ToString())));
                    tblPart.AddCell(CeldaVacia());   // Importe Autorizado — financiero lo llena
                }
                else
                {
                    for (int c = 0; c < 10; c++)
                        tblPart.AddCell(CeldaVacia(13f));
                }
            }

            doc.Add(tblPart);

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 5 — TOTALES
            // ════════════════════════════════════════════════════════════════
            var tblTot = new Table(UnitValue.CreatePointArray(new float[] { 100f, 120f, 142f, 100f, 58f }))
                .UseAllAvailableWidth();

            tblTot.AddCell(CeldaGris("Total Solicitado:", size: 7f, align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca("$", align: TextAlignment.LEFT));
            tblTot.AddCell(new Cell().SetBorder(Border.NO_BORDER));  // espacio
            tblTot.AddCell(CeldaGris("Total Autorizado:", size: 7f, align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca("$", align: TextAlignment.LEFT));

            doc.Add(tblTot);

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 6 — No. REQUISICIÓN
            // ════════════════════════════════════════════════════════════════
            var tblNumReq = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();

            // Celda "No. Requisición" con el número embebido
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(gris).SetPadding(2f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("No. Requisición:  ").SetFont(bold).SetFontSize(6f)
                    .Add(new Text(S(requisicion.NumRequisicion)).SetFont(regular))));
            tblNumReq.AddCell(CeldaGris("Oficio Suficiencia / Autorización:", align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));
            tblNumReq.AddCell(CeldaGris("Contrato Asociado:", align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));

            doc.Add(tblNumReq);

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 7 — COMENTARIOS
            // ════════════════════════════════════════════════════════════════
            var tblCom = new Table(UnitValue.CreatePointArray(new float[] { 520f }))
                .UseAllAvailableWidth();
            tblCom.AddCell(CeldaGris("COMENTARIOS:", align: TextAlignment.LEFT)
                .SetHeight(20f).SetPaddingLeft(4f));
            doc.Add(tblCom);

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 8 — FIRMAS
            // ════════════════════════════════════════════════════════════════
            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();

            var firmantes = new (string Titulo, string Nombre)[]
            {
                ("ASIGNACIÓN PRESUPUESTAL",
                 "JOEL MARTÍNEZ PÉREZ\nJEFE DEL DEPARTAMENTO DE RECURSOS\nFINANCIEROS"),
                ("Vo.Bo.",
                 "C. MARCOS MATAMOROS MORENO\nDIRECTOR DE ADMINISTRACIÓN Y FINANZAS"),
                ("AUTORIZÓ",
                 "C. CIRO MIGUEL JUÁREZ PALACIOS\nTITULAR DE LA UNIDAD DE PLANEACIÓN,\nADMINISTRACIÓN Y FINANZAS"),
            };

            foreach (var (titulo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell()
                    .SetHeight(65f)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPadding(3f)
                    .Add(new Paragraph(titulo).SetFont(bold).SetFontSize(5.5f).SetMarginBottom(2f))
                    .Add(new Paragraph(" ").SetFontSize(18f))   // espacio para firma
                    .Add(new Paragraph("_____________________________")
                        .SetFont(regular).SetFontSize(5f).SetMarginBottom(2f))
                    .Add(new Paragraph(nombre.Replace("\n", " "))
                        .SetFont(regular).SetFontSize(4.8f)
                        .SetTextAlignment(TextAlignment.CENTER)));
            }

            doc.Add(tblFirmas);

            // ── Cerrar y devolver bytes ──────────────────────────────────────
            // doc.Close() llama a pdfDoc.Close() que llama a pdfWriter.Close().
            // El PdfWriter cierra el Stream subyacente, PERO el MemoryStream
            // sigue accesible mediante ToArray() porque MemoryStream.Close()
            // no limpia el buffer interno.
            doc.Close();

            return ms.ToArray();
        }

        public async Task<TablaApiEditableDTO> ObtenerTablaApiEditableAsync(int idRequisicion)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion)
                ?? throw new Exception($"No se encontró la requisición {idRequisicion}.");

            string nombreDepartamento = "";
            if (requisicion.IdDepartamento.HasValue)
            {
                var depto = await _repoDepartamento.Obtener(d => d.IdDepartamento == requisicion.IdDepartamento.Value);
                nombreDepartamento = depto?.NombreDepartamento ?? "";
            }

            var queryDet = await _repoDetalle.Consultar(d => d.IdRequisicion == idRequisicion);
            var detalles = await queryDet.ToListAsync();

            var fecha = requisicion.FechaEmision.HasValue
                ? requisicion.FechaEmision.Value.ToDateTime(TimeOnly.MinValue)
                : DateTime.Now;

            var modelo = new TablaApiEditableDTO
            {
                IdRequisicion = idRequisicion,
                FechaElaboracion = fecha.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-MX")),
                Ejercicio = DateTime.Now.Year.ToString(),
                AreaSolicitanteClave = S(requisicion.IdDepartamento?.ToString()),
                AreaSolicitanteNombre = nombreDepartamento,
                DescripcionBienServicio = S(requisicion.UsoEspecifico),
                Justificacion = S(requisicion.Justificacion),
                NumeroRequisicion = S(requisicion.NumRequisicion),
                OficioSuficiencia = "",
                ContratoAsociado = "",
                Comentarios = "",
                TotalSolicitado = "$",
                TotalAutorizado = "$"
            };

            foreach (var d in detalles)
            {
                modelo.Partidas.Add(new TablaApiPartidaEditableDTO
                {
                    Numero = (modelo.Partidas.Count + 1).ToString(),
                    Ua = S(requisicion.IdDepartamento?.ToString()),
                    ClaveMunicipio = S(requisicion.ClaveRegion?.ToString()),
                    ImporteSolicitado = "",
                    FuenteFinanciamiento = S(requisicion.Ff),
                    Pp = S(requisicion.IdPp?.ToString()),
                    Componente = "",
                    Actividad = "",
                    ObjetoGasto = S(d.CogEditable?.ToString() ?? d.NumPartida?.ToString()),
                    ImporteAutorizado = ""
                });
            }

            return modelo;
        }

        public async Task<byte[]> GenerarTablaApiAsync(TablaApiEditableDTO modelo)
        {
            // Reusar datos base para completar campos faltantes y mantener consistencia.
            var baseModelo = await ObtenerTablaApiEditableAsync(modelo.IdRequisicion);
            var finalModel = CombinarModeloTablaApi(baseModelo, modelo);

            var ms = new MemoryStream();
            var pdfWriter = new PdfWriter(ms);
            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, PageSize.LETTER);
            doc.SetMargins(10f, 10f, 10f, 10f);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var gris = new DeviceGray(0.82f);

            Cell CeldaGris(string texto, int colspan = 1, int rowspan = 1, float size = 6f,
                           TextAlignment align = TextAlignment.CENTER)
            {
                var p = new Paragraph(S(texto)).SetFont(bold).SetFontSize(size);
                return new Cell(rowspan, colspan)
                    .SetBackgroundColor(gris)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(2f)
                    .Add(p);
            }

            Cell CeldaBlanca(string texto, float size = 6f, bool negrita = false,
                             TextAlignment align = TextAlignment.CENTER)
            {
                var p = new Paragraph(S(texto)).SetFont(negrita ? bold : regular).SetFontSize(size);
                return new Cell()
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(2f)
                    .Add(p);
            }

            Cell CeldaVacia(float height = 14f) =>
                new Cell().SetHeight(height).SetPadding(0f);

            var tblEnc = new Table(UnitValue.CreatePointArray(new float[] { 90f, 320f, 110f }))
                .UseAllAvailableWidth();
            tblEnc.AddCell(new Cell(5, 1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .Add(new Paragraph("PUEBLA").SetFont(bold).SetFontSize(11))
                .Add(new Paragraph("Gobierno del Estado").SetFont(regular).SetFontSize(6))
                .Add(new Paragraph("2 0 2 4 - 2 0 3 0").SetFont(regular).SetFontSize(5)));
            tblEnc.AddCell(new Cell()
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(3f)
                .Add(new Paragraph("Autorización Presupuestal Interna").SetFont(bold).SetFontSize(11))
                .Add(new Paragraph("Dirección de Administración y Finanzas").SetFont(regular).SetFontSize(8))
                .Add(new Paragraph("Departamento de Recursos Materiales y Servicios Generales").SetFont(regular).SetFontSize(7))
                .Add(new Paragraph("Departamento de Recursos Financieros").SetFont(bold).SetFontSize(7))
                .Add(new Paragraph($"EJERCICIO {S(finalModel.Ejercicio)}").SetFont(bold).SetFontSize(8)));
            tblEnc.AddCell(new Cell(5, 1)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .Add(new Paragraph("Familias").SetFont(bold).SetFontSize(12))
                .Add(new Paragraph("Sistema Estatal DIF").SetFont(regular).SetFontSize(7))
                .Add(new Paragraph(" ").SetFontSize(4f))
                .Add(new Paragraph("Fecha de elaboración:").SetFont(bold).SetFontSize(6))
                .Add(new Paragraph(S(finalModel.FechaElaboracion)).SetFont(regular).SetFontSize(5.5f)));
            for (int i = 0; i < 4; i++) tblEnc.AddCell(new Cell().SetHeight(10f).SetPadding(0f));
            doc.Add(tblEnc);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            var tblArea = new Table(UnitValue.CreatePointArray(new float[] { 85f, 38f, 397f })).UseAllAvailableWidth();
            tblArea.AddCell(CeldaGris("ÁREA SOLICITANTE:", align: TextAlignment.LEFT));
            tblArea.AddCell(CeldaBlanca(finalModel.AreaSolicitanteClave, negrita: true));
            tblArea.AddCell(CeldaBlanca(finalModel.AreaSolicitanteNombre, align: TextAlignment.LEFT));
            foreach (var lbl in new[] { "ADECUACIÓN:", "AUTORIZACIÓN:", "SOLICITUD DE PAGO:" })
            {
                tblArea.AddCell(CeldaGris(lbl, align: TextAlignment.LEFT));
                tblArea.AddCell(new Cell(1, 2).SetHeight(10f).SetPadding(0f));
            }
            doc.Add(tblArea);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            var tblBien = new Table(UnitValue.CreatePointArray(new float[] { 105f, 415f })).UseAllAvailableWidth();
            tblBien.AddCell(new Cell(1, 2).SetBackgroundColor(gris).SetFont(bold).SetFontSize(6.5f)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(2f)
                .Add(new Paragraph("INFORMACIÓN DEL BIEN O SERVICIO POR ADQUIRIR")));
            tblBien.AddCell(CeldaGris("DESCRIPCIÓN DETALLADA\nDEL BIEN O SERVICIO:").SetHeight(28f));
            tblBien.AddCell(new Cell().SetFont(regular).SetFontSize(7f).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(3f)
                .Add(new Paragraph(S(finalModel.DescripcionBienServicio))));
            tblBien.AddCell(CeldaGris("JUSTIFICACIÓN:").SetHeight(28f));
            tblBien.AddCell(new Cell().SetFont(regular).SetFontSize(6.5f).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(3f)
                .Add(new Paragraph(S(finalModel.Justificacion))));
            doc.Add(tblBien);
            doc.Add(new Paragraph("").SetMarginBottom(1f));

            var colWidths = new float[] { 20f, 26f, 36f, 52f, 50f, 30f, 42f, 36f, 40f, 50f };
            var tblPart = new Table(UnitValue.CreatePointArray(colWidths)).UseAllAvailableWidth();
            tblPart.AddHeaderCell(new Cell(1, 4).SetBackgroundColor(gris).SetFont(bold).SetFontSize(5.5f).SetTextAlignment(TextAlignment.CENTER).SetPadding(2f)
                .Add(new Paragraph("SOLICITUD DE SUFICIENCIA")));
            tblPart.AddHeaderCell(new Cell(1, 6).SetBackgroundColor(gris).SetFont(bold).SetFontSize(5.5f).SetTextAlignment(TextAlignment.CENTER).SetPadding(2f)
                .Add(new Paragraph("AUTORIZACIÓN LA SECCIÓN DE PROGRAMACIÓN PRESUPUESTAL Y FINANCIERA")));
            var colTitles = new[] { "No.", "UA", "CLAVE\nMUNICIPIO", "IMPORTE\nSOLICITADO", "FUENTE DE\nFINANCIAMIENTO", "PP", "COMPONENTE", "ACTIVIDAD", "OBJETO\nDEL GASTO", "IMPORTE\nAUTORIZADO" };
            foreach (var h in colTitles) tblPart.AddHeaderCell(CeldaGris(h, size: 5f).SetHeight(18f));

            int numFilas = Math.Max(15, finalModel.Partidas.Count + 3);
            for (int i = 0; i < numFilas; i++)
            {
                if (i < finalModel.Partidas.Count)
                {
                    var d = finalModel.Partidas[i];
                    tblPart.AddCell(CeldaBlanca(d.Numero));
                    tblPart.AddCell(CeldaBlanca(d.Ua));
                    tblPart.AddCell(CeldaBlanca(d.ClaveMunicipio));
                    tblPart.AddCell(CeldaBlanca(d.ImporteSolicitado));
                    tblPart.AddCell(CeldaBlanca(d.FuenteFinanciamiento));
                    tblPart.AddCell(CeldaBlanca(d.Pp));
                    tblPart.AddCell(CeldaBlanca(d.Componente));
                    tblPart.AddCell(CeldaBlanca(d.Actividad));
                    tblPart.AddCell(CeldaBlanca(d.ObjetoGasto));
                    tblPart.AddCell(CeldaBlanca(d.ImporteAutorizado));
                }
                else
                {
                    for (int c = 0; c < 10; c++) tblPart.AddCell(CeldaVacia(13f));
                }
            }
            doc.Add(tblPart);

            var tblTot = new Table(UnitValue.CreatePointArray(new float[] { 100f, 120f, 142f, 100f, 58f })).UseAllAvailableWidth();
            tblTot.AddCell(CeldaGris("Total Solicitado:", size: 7f, align: TextAlignment.LEFT).SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca(S(finalModel.TotalSolicitado), align: TextAlignment.LEFT));
            tblTot.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            tblTot.AddCell(CeldaGris("Total Autorizado:", size: 7f, align: TextAlignment.LEFT).SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca(S(finalModel.TotalAutorizado), align: TextAlignment.LEFT));
            doc.Add(tblTot);

            var tblNumReq = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f })).UseAllAvailableWidth();
            tblNumReq.AddCell(new Cell().SetBackgroundColor(gris).SetPadding(2f).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("No. Requisición:  ").SetFont(bold).SetFontSize(6f).Add(new Text(S(finalModel.NumeroRequisicion)).SetFont(regular))));
            tblNumReq.AddCell(new Cell().SetBackgroundColor(gris).SetPadding(2f).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("Oficio Suficiencia / Autorización: ").SetFont(bold).SetFontSize(6f).Add(new Text(S(finalModel.OficioSuficiencia)).SetFont(regular))));
            tblNumReq.AddCell(new Cell().SetBackgroundColor(gris).SetPadding(2f).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(new Paragraph("Contrato Asociado: ").SetFont(bold).SetFontSize(6f).Add(new Text(S(finalModel.ContratoAsociado)).SetFont(regular))));
            doc.Add(tblNumReq);

            var tblCom = new Table(UnitValue.CreatePointArray(new float[] { 520f })).UseAllAvailableWidth();
            tblCom.AddCell(new Cell().SetBackgroundColor(gris).SetHeight(20f).SetPaddingLeft(4f).SetPaddingTop(3f)
                .Add(new Paragraph("COMENTARIOS: ").SetFont(bold).SetFontSize(6f).Add(new Text(S(finalModel.Comentarios)).SetFont(regular))));
            doc.Add(tblCom);

            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f })).UseAllAvailableWidth();
            var firmantes = new (string Titulo, string Nombre)[]
            {
                ("ASIGNACIÓN PRESUPUESTAL", "JOEL MARTÍNEZ PÉREZ\nJEFE DEL DEPARTAMENTO DE RECURSOS\nFINANCIEROS"),
                ("Vo.Bo.", "C. MARCOS MATAMOROS MORENO\nDIRECTOR DE ADMINISTRACIÓN Y FINANZAS"),
                ("AUTORIZÓ", "C. CIRO MIGUEL JUÁREZ PALACIOS\nTITULAR DE LA UNIDAD DE PLANEACIÓN,\nADMINISTRACIÓN Y FINANZAS"),
            };
            foreach (var (titulo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell().SetHeight(65f).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.TOP).SetPadding(3f)
                    .Add(new Paragraph(titulo).SetFont(bold).SetFontSize(5.5f).SetMarginBottom(2f))
                    .Add(new Paragraph(" ").SetFontSize(18f))
                    .Add(new Paragraph("_____________________________").SetFont(regular).SetFontSize(5f).SetMarginBottom(2f))
                    .Add(new Paragraph(nombre.Replace("\n", " ")).SetFont(regular).SetFontSize(4.8f).SetTextAlignment(TextAlignment.CENTER)));
            }
            doc.Add(tblFirmas);

            doc.Close();
            return ms.ToArray();
        }

        private static TablaApiEditableDTO CombinarModeloTablaApi(TablaApiEditableDTO baseModelo, TablaApiEditableDTO entrada)
        {
            var salida = new TablaApiEditableDTO
            {
                IdRequisicion = baseModelo.IdRequisicion,
                FechaElaboracion = string.IsNullOrWhiteSpace(entrada.FechaElaboracion) ? baseModelo.FechaElaboracion : entrada.FechaElaboracion,
                Ejercicio = string.IsNullOrWhiteSpace(entrada.Ejercicio) ? baseModelo.Ejercicio : entrada.Ejercicio,
                AreaSolicitanteClave = string.IsNullOrWhiteSpace(entrada.AreaSolicitanteClave) ? baseModelo.AreaSolicitanteClave : entrada.AreaSolicitanteClave,
                AreaSolicitanteNombre = string.IsNullOrWhiteSpace(entrada.AreaSolicitanteNombre) ? baseModelo.AreaSolicitanteNombre : entrada.AreaSolicitanteNombre,
                DescripcionBienServicio = string.IsNullOrWhiteSpace(entrada.DescripcionBienServicio) ? baseModelo.DescripcionBienServicio : entrada.DescripcionBienServicio,
                Justificacion = string.IsNullOrWhiteSpace(entrada.Justificacion) ? baseModelo.Justificacion : entrada.Justificacion,
                NumeroRequisicion = string.IsNullOrWhiteSpace(entrada.NumeroRequisicion) ? baseModelo.NumeroRequisicion : entrada.NumeroRequisicion,
                OficioSuficiencia = entrada.OficioSuficiencia ?? "",
                ContratoAsociado = entrada.ContratoAsociado ?? "",
                Comentarios = entrada.Comentarios ?? "",
                TotalSolicitado = string.IsNullOrWhiteSpace(entrada.TotalSolicitado) ? baseModelo.TotalSolicitado : entrada.TotalSolicitado,
                TotalAutorizado = string.IsNullOrWhiteSpace(entrada.TotalAutorizado) ? baseModelo.TotalAutorizado : entrada.TotalAutorizado,
                Partidas = new List<TablaApiPartidaEditableDTO>()
            };

            var origen = (entrada.Partidas != null && entrada.Partidas.Any()) ? entrada.Partidas : baseModelo.Partidas;
            for (int i = 0; i < origen.Count; i++)
            {
                var basePartida = i < baseModelo.Partidas.Count ? baseModelo.Partidas[i] : new TablaApiPartidaEditableDTO();
                var inPartida = origen[i] ?? new TablaApiPartidaEditableDTO();
                salida.Partidas.Add(new TablaApiPartidaEditableDTO
                {
                    Numero = string.IsNullOrWhiteSpace(inPartida.Numero) ? basePartida.Numero : inPartida.Numero,
                    Ua = string.IsNullOrWhiteSpace(inPartida.Ua) ? basePartida.Ua : inPartida.Ua,
                    ClaveMunicipio = string.IsNullOrWhiteSpace(inPartida.ClaveMunicipio) ? basePartida.ClaveMunicipio : inPartida.ClaveMunicipio,
                    ImporteSolicitado = inPartida.ImporteSolicitado ?? "",
                    FuenteFinanciamiento = string.IsNullOrWhiteSpace(inPartida.FuenteFinanciamiento) ? basePartida.FuenteFinanciamiento : inPartida.FuenteFinanciamiento,
                    Pp = string.IsNullOrWhiteSpace(inPartida.Pp) ? basePartida.Pp : inPartida.Pp,
                    Componente = inPartida.Componente ?? "",
                    Actividad = inPartida.Actividad ?? "",
                    ObjetoGasto = string.IsNullOrWhiteSpace(inPartida.ObjetoGasto) ? basePartida.ObjetoGasto : inPartida.ObjetoGasto,
                    ImporteAutorizado = inPartida.ImporteAutorizado ?? ""
                });
            }

            return salida;
        }

        // Helper: convierte null → string vacío
        private static string S(string? valor) => valor ?? "";

        private async Task<string> GenerarNumeroApiAsync()
        {
            int anioActual = DateTime.Now.Year;
            // Los dos últimos dígitos del año: 2026 → "26"
            string sufAno = (anioActual % 100).ToString("D2");

            // Prefijo que tienen todos los números API de este año
            // Formato guardado en BD: "API-0001/26"
            string prefijo = $"API-";
            string terminacion = $"/{sufAno}";

            // Obtener todos los NumApi del año en curso que tengan el formato esperado
            var query = await _repositoryRequisicion.Consultar(r =>
                r.NumApi != null &&
                r.NumApi.StartsWith(prefijo) &&
                r.NumApi.EndsWith(terminacion));

            var registros = await query.Select(r => r.NumApi).ToListAsync();

            // Extraer el número consecutivo más alto
            // Ejemplo: "API-0042/26" → 42
            int maxConsecutivo = 0;
            foreach (var numApi in registros)
            {
                // numApi tiene forma "API-XXXX/YY"
                // Extraemos lo que está entre "API-" y "/YY"
                var inicio = prefijo.Length;                      // posición después de "API-"
                var fin = numApi.Length - terminacion.Length;  // posición antes de "/26"

                if (fin > inicio)
                {
                    var parteNumerica = numApi.Substring(inicio, fin - inicio);
                    if (int.TryParse(parteNumerica, out int num) && num > maxConsecutivo)
                        maxConsecutivo = num;
                }
            }

            int siguiente = maxConsecutivo + 1;

            // Formato final: API-0001/26  (4 dígitos con ceros)
            return $"API-{siguiente:D4}/{sufAno}";
        }
    }
}
