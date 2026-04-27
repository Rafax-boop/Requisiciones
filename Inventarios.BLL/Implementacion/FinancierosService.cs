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
        /// <summary>Paleta alineada con formulario-requisiciones.css (PDF de requisiciones).</summary>
        private static class PdfApiEstiloRequi
        {
            public static readonly DeviceRgb FondoEncabezadoTabla = new(248, 250, 252);
            public static readonly DeviceRgb Borde = new(226, 232, 240);
            public static readonly DeviceRgb BordeCuadro = new(203, 213, 225);
            public static readonly DeviceRgb TextoPrincipal = new(26, 26, 26);
            public static readonly DeviceRgb TextoSecundario = new(100, 116, 139);
            public static readonly DeviceRgb TextoEncabezadoTabla = new(71, 85, 105);
            public static readonly DeviceRgb TextoInstitucional = new(45, 45, 45);
            public static readonly DeviceRgb RosaAcento = new(255, 45, 111);
        }

        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblBitacoraEstatus> _repositoryBitacora;
        private readonly IGenericRepository<TblRegistroDiseno> _repositoryDiseno;
        private readonly IGenericRepository<TblDepartamento> _repoDepartamento;
        private readonly IGenericRepository<TblRequisicionDetalle> _repoDetalle;
        private readonly IGenericRepository<TblCotizacione> _repositoryCotizaciones;
        private readonly IGenericRepository<TblTablaApiHistorial> _repoHistorial;

        public FinancierosService(
            IRequisicionRepository repositoryRequisicion,
            IGenericRepository<TblBitacoraEstatus> repositoryBitacora,
            IGenericRepository<TblRegistroDiseno> repositoryDiseno,
            IGenericRepository<TblDepartamento> repoDepartamento,
            IGenericRepository<TblRequisicionDetalle> repoDetalle,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblTablaApiHistorial> repoHistorial)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDiseno = repositoryDiseno;
            _repoDepartamento = repoDepartamento;
            _repoDetalle = repoDetalle;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repoHistorial = repoHistorial;
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

        public async Task<bool> FinalizarRequisicion(int idRequisicion, List<IFormFile>? transferencias, int idUsuario)
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

                if (transferencias != null && transferencias.Any())
                {
                    var rutaBase = System.IO.Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "uploads", "Transferencias", idRequisicion.ToString());
                    Directory.CreateDirectory(rutaBase);

                    foreach (var archivo in transferencias)
                    {
                        if (archivo.Length == 0) continue;

                        var nombreArchivo = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(archivo.FileName)}";
                        var rutaFisica = System.IO.Path.Combine(rutaBase, nombreArchivo);

                        using (var stream = new FileStream(rutaFisica, FileMode.Create))
                            await archivo.CopyToAsync(stream);

                        var registro = new TblRegistroDiseno
                        {
                            IdRequisicion = idRequisicion,
                            Ruta = $"/uploads/Transferencias/{idRequisicion}/{nombreArchivo}",
                            FechaSubida = DateTime.Now,
                            Tipo = "Transferencia"
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
            var cotizacionConCantidad = await ObtenerCotizacionConCantidadAsync(idRequisicion);

            // Importe final por partida (precio × cantidad × IVA)
            var importePorPartida = cotizacionConCantidad.ToDictionary(
                kv => kv.Key,
                kv => (bool)kv.Value.IVA
                    ? kv.Value.PrecioUnitario * kv.Value.Cantidad * 1.16m
                    : kv.Value.PrecioUnitario * kv.Value.Cantidad);

            decimal totalSolicitado = importePorPartida.Values.Any() ? importePorPartida.Values.Sum() : 0;

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
            doc.SetMargins(22f, 34f, 28f, 34f);

            // Fuentes estándar embebidas — no requieren archivos en disco
            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var bordeCelda = new SolidBorder(PdfApiEstiloRequi.Borde, 0.75f);
            // ── Helpers locales (estética tipo PDF requisiciones) ───────────
            Cell CeldaGris(string texto, int colspan = 1, int rowspan = 1, float size = 6f,
                           TextAlignment align = TextAlignment.CENTER, bool textoEstiloColumna = false)
            {
                var colorTxt = textoEstiloColumna ? PdfApiEstiloRequi.TextoEncabezadoTabla : PdfApiEstiloRequi.TextoSecundario;
                var p = new Paragraph(S(texto)).SetFont(bold).SetFontSize(size).SetFontColor(colorTxt);
                return new Cell(rowspan, colspan)
                    .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(4f)
                    .SetBorder(bordeCelda)
                    .Add(p);
            }

            Cell CeldaBlanca(string texto, float size = 6f, bool negrita = false,
                             TextAlignment align = TextAlignment.CENTER, Color? fondoFila = null)
            {
                var p = new Paragraph(S(texto)).SetFont(negrita ? bold : regular).SetFontSize(size)
                    .SetFontColor(PdfApiEstiloRequi.TextoPrincipal);
                return new Cell()
                    .SetBackgroundColor(fondoFila ?? ColorConstants.WHITE)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(4f)
                    .SetBorder(bordeCelda)
                    .Add(p);
            }

            Cell CeldaVacia(float height = 14f, Color? fondoFila = null) =>
                new Cell()
                    .SetHeight(height)
                    .SetPadding(0f)
                    .SetBorder(bordeCelda)
                    .SetBackgroundColor(fondoFila ?? ColorConstants.WHITE);

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
            // BLOQUE 1 — ENCABEZADO (logos laterales + título)
            // ════════════════════════════════════════════════════════════════
            var tblEnc = CrearTablaEncabezadoApi(bold, regular, fechaStr, DateTime.Now.Year.ToString());
            doc.Add(tblEnc);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

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
                tblArea.AddCell(new Cell(1, 2).SetHeight(10f).SetPadding(0f).SetBorder(bordeCelda)
                    .SetBackgroundColor(ColorConstants.WHITE));
            }

            doc.Add(tblArea);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 3 — INFORMACIÓN DEL BIEN O SERVICIO
            // ════════════════════════════════════════════════════════════════
            var tblBien = new Table(UnitValue.CreatePointArray(new float[] { 105f, 415f }))
                .UseAllAvailableWidth();

            tblBien.AddCell(new Cell(1, 2)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("INFORMACIÓN DEL BIEN O SERVICIO POR ADQUIRIR")
                    .SetFont(bold).SetFontSize(7f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));

            tblBien.AddCell(CeldaGris("DESCRIPCIÓN DETALLADA\nDEL BIEN O SERVICIO:").SetHeight(22f));
            tblBien.AddCell(new Cell()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetFont(regular).SetFontSize(8f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph(S(requisicion.UsoEspecifico)).SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));

            tblBien.AddCell(CeldaGris("JUSTIFICACIÓN:").SetHeight(22f));
            tblBien.AddCell(new Cell()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetFont(regular).SetFontSize(7.5f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph(S(requisicion.Justificacion)).SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));

            doc.Add(tblBien);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 4 — TABLA DE PARTIDAS
            // ════════════════════════════════════════════════════════════════
            // Anchos de columna: No | UA | ClaveMun | ImpSol | FuenteFin | PP | Comp | Act | ObjGasto | ImpAut
            var colWidths = new float[] { 20f, 26f, 36f, 52f, 50f, 30f, 42f, 36f, 40f, 50f };
            var tblPart = new Table(UnitValue.CreatePointArray(colWidths)).UseAllAvailableWidth();

            // Sub-encabezados de sección (fila 1 del header)
            tblPart.AddHeaderCell(new Cell(1, 4)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("SOLICITUD DE SUFICIENCIA")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));

            tblPart.AddHeaderCell(new Cell(1, 6)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("AUTORIZACIÓN LA SECCIÓN DE PROGRAMACIÓN PRESUPUESTAL Y FINANCIERA")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));

            // Encabezados de columna (fila 2 del header)
            var colTitles = new[]
            {
                "No.", "UA", "CLAVE\nMUNICIPIO", "IMPORTE\nSOLICITADO",
                "FUENTE DE\nFINANCIAMIENTO", "PP", "COMPONENTE",
                "ACTIVIDAD", "OBJETO\nDEL GASTO", "IMPORTE\nAUTORIZADO"
            };
            foreach (var h in colTitles)
                tblPart.AddHeaderCell(CeldaGris(h, size: 5.25f, textoEstiloColumna: true).SetHeight(18f));

            // Filas de datos (zebra). Mínimo 8; crece con partidas (+1 fila de margen).
            int numFilas = Math.Max(8, detalles.Count + 1);
            for (int i = 0; i < numFilas; i++)
            {
                Color bg = (i % 2 == 1) ? PdfApiEstiloRequi.FondoEncabezadoTabla : ColorConstants.WHITE;
                if (i < detalles.Count)
                {
                    var d = detalles[i];
                    var importeStr = importePorPartida.TryGetValue(d.IdRequisicionDetalle, out var imp)
                        ? FormatearImporte(imp)
                        : "";

                    tblPart.AddCell(CeldaBlanca((i + 1).ToString(), fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.IdDepartamento?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.ClaveRegion?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(importeStr, fondoFila: bg));              // ← antes CeldaVacia
                    tblPart.AddCell(CeldaBlanca(S(requisicion.Ff), fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.IdPp?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                    tblPart.AddCell(CeldaBlanca(S(d.CogEditable?.ToString() ?? d.NumPartida?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                }
                else
                {
                    for (int c = 0; c < 10; c++)
                        tblPart.AddCell(CeldaVacia(10f, bg));
                }
            }

            doc.Add(tblPart);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 5 — TOTALES
            // ════════════════════════════════════════════════════════════════

            var totalSolicitadoStr = totalSolicitado > 0 ? FormatearImporte(totalSolicitado) : "$";
            var tblTot = new Table(UnitValue.CreatePointArray(new float[] { 100f, 120f, 142f, 100f, 58f }))
                .UseAllAvailableWidth();

            tblTot.AddCell(CeldaGris("Total Solicitado:", size: 7f, align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca(totalSolicitadoStr, align: TextAlignment.LEFT));
            tblTot.AddCell(new Cell().SetBorder(bordeCelda).SetBackgroundColor(ColorConstants.WHITE));
            tblTot.AddCell(CeldaGris("Total Autorizado:", size: 7f, align: TextAlignment.LEFT)
                .SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca("$", align: TextAlignment.LEFT));

            doc.Add(tblTot);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 6 — No. REQUISICIÓN
            // ════════════════════════════════════════════════════════════════
            var tblNumReq = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();

            // Celda "No. Requisición" con el número embebido
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("No. Requisición:  ").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .Add(new Text(S(requisicion.NumRequisicion)).SetFont(regular).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))));
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("Oficio Suficiencia / Autorización:").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)));
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("Contrato Asociado:").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)));

            doc.Add(tblNumReq);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            // ════════════════════════════════════════════════════════════════
            // BLOQUE 7 — COMENTARIOS
            // ════════════════════════════════════════════════════════════════
            var tblCom = new Table(UnitValue.CreatePointArray(new float[] { 520f }))
                .UseAllAvailableWidth();
            tblCom.AddCell(CeldaGris("COMENTARIOS:", align: TextAlignment.LEFT)
                .SetHeight(18f).SetPaddingLeft(4f).SetPaddingTop(3f));
            doc.Add(tblCom);
            doc.Add(new Paragraph("").SetMarginBottom(8f));

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
                    .SetMinHeight(78f)
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPaddingTop(10f)
                    .SetPaddingBottom(10f)
                    .SetPaddingLeft(6f)
                    .SetPaddingRight(6f)
                    .SetBorder(bordeCelda)
                    .Add(new Paragraph(titulo).SetFont(bold).SetFontSize(5.75f)
                        .SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla).SetMarginBottom(10f))
                    .Add(new Paragraph(" ").SetFontSize(22f))
                    .Add(new Paragraph("_________________________________________")
                        .SetFont(regular).SetFontSize(5f).SetFontColor(PdfApiEstiloRequi.TextoPrincipal).SetMarginBottom(6f))
                    .Add(new Paragraph(nombre.Replace("\n", " "))
                        .SetFont(regular).SetFontSize(5f)
                        .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
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

            // ← NUEVO
            var cotizacionConCantidad = await ObtenerCotizacionConCantidadAsync(idRequisicion);

            var importePorPartida = cotizacionConCantidad.ToDictionary(
                kv => kv.Key,
                kv => (bool)kv.Value.IVA
                    ? kv.Value.PrecioUnitario * kv.Value.Cantidad * 1.16m
                    : kv.Value.PrecioUnitario * kv.Value.Cantidad);

            decimal totalSolicitado = importePorPartida.Values.Any() ? importePorPartida.Values.Sum() : 0;

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
                TotalSolicitado = totalSolicitado > 0 ? FormatearImporte(totalSolicitado) : "$",
                TotalAutorizado = "$"
            };

            foreach (var d in detalles)
            {
                var importeStr = importePorPartida.TryGetValue(d.IdRequisicionDetalle, out var imp)
                        ? FormatearImporte(imp)
                        : "";

                modelo.Partidas.Add(new TablaApiPartidaEditableDTO
                {
                    Numero = (modelo.Partidas.Count + 1).ToString(),
                    Ua = S(requisicion.IdDepartamento?.ToString()),
                    ClaveMunicipio = S(requisicion.ClaveRegion?.ToString()),
                    ImporteSolicitado = importeStr,
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
            if (modelo.IdRequisicion <= 0)
                throw new ArgumentException("La requisición es requerida.", nameof(modelo));

            var requi = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == modelo.IdRequisicion);
            if (requi == null)
                throw new Exception($"No se encontró la requisición {modelo.IdRequisicion}.");

            // Solo lo capturado en el formulario; sin rellenar desde BD ni fusionar modelos.
            AsegurarTablaApiDesdeFormulario(modelo);

            var ms = new MemoryStream();
            var pdfWriter = new PdfWriter(ms);
            var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, PageSize.LETTER);
            doc.SetMargins(22f, 34f, 28f, 34f);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA,
                              iText.IO.Font.PdfEncodings.WINANSI, PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

            var bordeCelda = new SolidBorder(PdfApiEstiloRequi.Borde, 0.75f);

            Cell CeldaGris(string texto, int colspan = 1, int rowspan = 1, float size = 6f,
                           TextAlignment align = TextAlignment.CENTER, bool textoEstiloColumna = false)
            {
                var colorTxt = textoEstiloColumna ? PdfApiEstiloRequi.TextoEncabezadoTabla : PdfApiEstiloRequi.TextoSecundario;
                var p = new Paragraph(S(texto)).SetFont(bold).SetFontSize(size).SetFontColor(colorTxt);
                return new Cell(rowspan, colspan)
                    .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(4f)
                    .SetBorder(bordeCelda)
                    .Add(p);
            }

            Cell CeldaBlanca(string texto, float size = 6f, bool negrita = false,
                             TextAlignment align = TextAlignment.CENTER, Color? fondoFila = null)
            {
                var p = new Paragraph(S(texto)).SetFont(negrita ? bold : regular).SetFontSize(size)
                    .SetFontColor(PdfApiEstiloRequi.TextoPrincipal);
                return new Cell()
                    .SetBackgroundColor(fondoFila ?? ColorConstants.WHITE)
                    .SetTextAlignment(align)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetPadding(4f)
                    .SetBorder(bordeCelda)
                    .Add(p);
            }

            Cell CeldaVacia(float height = 14f, Color? fondoFila = null) =>
                new Cell()
                    .SetHeight(height)
                    .SetPadding(0f)
                    .SetBorder(bordeCelda)
                    .SetBackgroundColor(fondoFila ?? ColorConstants.WHITE);

            var ejercicioPdf = S(modelo.Ejercicio).Trim();
            var tblEnc = CrearTablaEncabezadoApi(bold, regular, S(modelo.FechaElaboracion), ejercicioPdf);
            doc.Add(tblEnc);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var tblArea = new Table(UnitValue.CreatePointArray(new float[] { 85f, 38f, 397f })).UseAllAvailableWidth();
            tblArea.AddCell(CeldaGris("ÁREA SOLICITANTE:", align: TextAlignment.LEFT));
            tblArea.AddCell(CeldaBlanca(modelo.AreaSolicitanteClave, negrita: true));
            tblArea.AddCell(CeldaBlanca(modelo.AreaSolicitanteNombre, align: TextAlignment.LEFT));
            foreach (var lbl in new[] { "ADECUACIÓN:", "AUTORIZACIÓN:", "SOLICITUD DE PAGO:" })
            {
                tblArea.AddCell(CeldaGris(lbl, align: TextAlignment.LEFT));
                tblArea.AddCell(new Cell(1, 2).SetHeight(10f).SetPadding(0f).SetBorder(bordeCelda)
                    .SetBackgroundColor(ColorConstants.WHITE));
            }
            doc.Add(tblArea);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var tblBien = new Table(UnitValue.CreatePointArray(new float[] { 105f, 415f })).UseAllAvailableWidth();
            tblBien.AddCell(new Cell(1, 2)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("INFORMACIÓN DEL BIEN O SERVICIO POR ADQUIRIR")
                    .SetFont(bold).SetFontSize(7f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));
            tblBien.AddCell(CeldaGris("DESCRIPCIÓN DETALLADA\nDEL BIEN O SERVICIO:").SetHeight(22f));
            tblBien.AddCell(new Cell()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetFont(regular).SetFontSize(8f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph(S(modelo.DescripcionBienServicio)).SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));
            tblBien.AddCell(CeldaGris("JUSTIFICACIÓN:").SetHeight(22f));
            tblBien.AddCell(new Cell()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetFont(regular).SetFontSize(7.5f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph(S(modelo.Justificacion)).SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));
            doc.Add(tblBien);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var colWidths = new float[] { 20f, 26f, 36f, 52f, 50f, 30f, 42f, 36f, 40f, 50f };
            var tblPart = new Table(UnitValue.CreatePointArray(colWidths)).UseAllAvailableWidth();
            tblPart.AddHeaderCell(new Cell(1, 4)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("SOLICITUD DE SUFICIENCIA")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));
            tblPart.AddHeaderCell(new Cell(1, 6)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetTextAlignment(TextAlignment.CENTER).SetPadding(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("AUTORIZACIÓN LA SECCIÓN DE PROGRAMACIÓN PRESUPUESTAL Y FINANCIERA")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));
            var colTitles = new[] { "No.", "UA", "CLAVE\nMUNICIPIO", "IMPORTE\nSOLICITADO", "FUENTE DE\nFINANCIAMIENTO", "PP", "COMPONENTE", "ACTIVIDAD", "OBJETO\nDEL GASTO", "IMPORTE\nAUTORIZADO" };
            foreach (var h in colTitles) tblPart.AddHeaderCell(CeldaGris(h, size: 5.25f, textoEstiloColumna: true).SetHeight(18f));

            int numFilas = Math.Max(8, modelo.Partidas.Count + 1);
            for (int i = 0; i < numFilas; i++)
            {
                Color bg = (i % 2 == 1) ? PdfApiEstiloRequi.FondoEncabezadoTabla : ColorConstants.WHITE;
                if (i < modelo.Partidas.Count)
                {
                    var d = modelo.Partidas[i];
                    tblPart.AddCell(CeldaBlanca(d.Numero, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.Ua, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.ClaveMunicipio, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.ImporteSolicitado, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.FuenteFinanciamiento, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.Pp, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.Componente, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.Actividad, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.ObjetoGasto, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(d.ImporteAutorizado, fondoFila: bg));
                }
                else
                {
                    for (int c = 0; c < 10; c++) tblPart.AddCell(CeldaVacia(10f, bg));
                }
            }
            doc.Add(tblPart);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var tblTot = new Table(UnitValue.CreatePointArray(new float[] { 100f, 120f, 142f, 100f, 58f })).UseAllAvailableWidth();
            tblTot.AddCell(CeldaGris("Total Solicitado:", size: 7f, align: TextAlignment.LEFT).SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca(S(modelo.TotalSolicitado), align: TextAlignment.LEFT));
            tblTot.AddCell(new Cell().SetBorder(bordeCelda).SetBackgroundColor(ColorConstants.WHITE));
            tblTot.AddCell(CeldaGris("Total Autorizado:", size: 7f, align: TextAlignment.LEFT).SetPaddingLeft(4f));
            tblTot.AddCell(CeldaBlanca(S(modelo.TotalAutorizado), align: TextAlignment.LEFT));
            doc.Add(tblTot);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var tblNumReq = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f })).UseAllAvailableWidth();
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("No. Requisición:  ").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .Add(new Text(S(modelo.NumeroRequisicion)).SetFont(regular).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))));
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("Oficio Suficiencia / Autorización: ").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .Add(new Text(S(modelo.OficioSuficiencia)).SetFont(regular).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))));
            tblNumReq.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("Contrato Asociado: ").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .Add(new Text(S(modelo.ContratoAsociado)).SetFont(regular).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))));
            doc.Add(tblNumReq);
            doc.Add(new Paragraph("").SetMarginBottom(3f));

            var tblCom = new Table(UnitValue.CreatePointArray(new float[] { 520f })).UseAllAvailableWidth();
            tblCom.AddCell(new Cell()
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetMinHeight(18f).SetPaddingLeft(4f).SetPaddingTop(3f).SetPaddingRight(4f)
                .SetBorder(bordeCelda)
                .Add(new Paragraph("COMENTARIOS: ").SetFont(bold).SetFontSize(6.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .Add(new Text(S(modelo.Comentarios)).SetFont(regular).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))));
            doc.Add(tblCom);
            doc.Add(new Paragraph("").SetMarginBottom(8f));

            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f })).UseAllAvailableWidth();
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
                    .SetBorder(bordeCelda)
                    .Add(new Paragraph(titulo).SetFont(bold).SetFontSize(5.75f)
                        .SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla).SetMarginBottom(10f))
                    .Add(new Paragraph(" ").SetFontSize(22f))
                    .Add(new Paragraph("_________________________________________").SetFont(regular).SetFontSize(5f)
                        .SetFontColor(PdfApiEstiloRequi.TextoPrincipal).SetMarginBottom(6f))
                    .Add(new Paragraph(nombre.Replace("\n", " ")).SetFont(regular).SetFontSize(5f)
                        .SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                        .SetTextAlignment(TextAlignment.CENTER)));
            }
            doc.Add(tblFirmas);

            doc.Close();
            return ms.ToArray();
        }

        public async Task GuardarHistorialTablaApiAsync(
            TablaApiEditableDTO modelo,
            int idUsuario,
            string? observacion = null)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(modelo, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = false,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            var registro = new TblTablaApiHistorial
            {
                IdRequisicion = modelo.IdRequisicion,
                IdUsuario = idUsuario,
                FechaGeneracion = DateTime.Now,
                DatosJson = json,
                Observacion = observacion
            };

            await _repoHistorial.Crear(registro);
        }

        public async Task<List<TablaApiHistorialDTO>> ObtenerHistorialTablaApiAsync(int idRequisicion)
        {
            var query = await _repoHistorial.Consultar(h => h.IdRequisicion == idRequisicion);

            var lista = await query
                .OrderByDescending(h => h.FechaGeneracion)
                .Include(h => h.IdUsuarioNavigation)
                .Select(h => new
                {
                    h.IdHistorial,
                    h.IdRequisicion,
                    h.FechaGeneracion,
                    h.DatosJson,
                    h.Observacion,
                    NombreUsuario = h.IdUsuarioNavigation.Usuario
                })
                .ToListAsync();

            return lista.Select(h => new TablaApiHistorialDTO
            {
                IdHistorial = h.IdHistorial,
                IdRequisicion = h.IdRequisicion,
                FechaGeneracion = h.FechaGeneracion,
                NombreUsuario = h.NombreUsuario,
                Observacion = h.Observacion,
                Modelo = System.Text.Json.JsonSerializer
                                        .Deserialize<TablaApiEditableDTO>(h.DatosJson)
            }).ToList();
        }

        private Table CrearTablaEncabezadoApi(PdfFont bold, PdfFont regular, string fechaElaboracion, string ejercicioAnio)
        {
            var borde = new SolidBorder(PdfApiEstiloRequi.Borde, 1f);
            var bordeFecha = new SolidBorder(PdfApiEstiloRequi.BordeCuadro, 1.5f);

            var tblEnc = new Table(UnitValue.CreatePointArray(new float[] { 100f, 292f, 120f }))
                .UseAllAvailableWidth();
            tblEnc.SetBorder(borde);
            tblEnc.SetBackgroundColor(ColorConstants.WHITE);

            var celdaIzq = new Cell(1, 1)
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(8f)
                .SetBorder(borde);

            if (!IntentarAgregarLogoEnCelda(celdaIzq,
                    new[] { "corazon.png", "pensargrande.png", "familias-dif.png" }, 64f, 48f))
            {
                celdaIzq.Add(new Paragraph("PUEBLA").SetFont(bold).SetFontSize(10f).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))
                    .Add(new Paragraph("Gobierno del Estado").SetFont(regular).SetFontSize(6.5f).SetFontColor(PdfApiEstiloRequi.TextoSecundario))
                    .Add(new Paragraph("2 0 2 4 - 2 0 3 0").SetFont(regular).SetFontSize(5.5f).SetFontColor(PdfApiEstiloRequi.TextoSecundario));
            }

            tblEnc.AddCell(celdaIzq);

            var centro = new Cell()
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetVerticalAlignment(VerticalAlignment.TOP)
                .SetPadding(10f)
                .SetBorder(borde);
            centro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA")
                .SetFont(bold).SetFontSize(7.5f).SetFontColor(PdfApiEstiloRequi.TextoInstitucional)
                .SetTextAlignment(TextAlignment.CENTER));
            centro.Add(new Paragraph("DEL ESTADO DE PUEBLA")
                .SetFont(bold).SetFontSize(7.5f).SetFontColor(PdfApiEstiloRequi.TextoInstitucional)
                .SetTextAlignment(TextAlignment.CENTER));
            centro.Add(new Paragraph("Autorización Presupuestal Interna")
                .SetFont(bold).SetFontSize(12.5f).SetFontColor(PdfApiEstiloRequi.TextoPrincipal)
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(5f));
            centro.Add(new Paragraph("Dirección de Administración y Finanzas")
                .SetFont(regular).SetFontSize(8f).SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(2f));
            centro.Add(new Paragraph("Departamento de Recursos Materiales y Servicios Generales")
                .SetFont(regular).SetFontSize(7.5f).SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                .SetTextAlignment(TextAlignment.CENTER));
            centro.Add(new Paragraph("Departamento de Recursos Financieros")
                .SetFont(bold).SetFontSize(7.5f).SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                .SetTextAlignment(TextAlignment.CENTER));
            centro.Add(new Paragraph($"EJERCICIO {S(ejercicioAnio)}")
                .SetFont(bold).SetFontSize(10f).SetFontColor(PdfApiEstiloRequi.RosaAcento)
                .SetTextAlignment(TextAlignment.CENTER).SetMarginTop(5f));
            tblEnc.AddCell(centro);

            var celdaDer = new Cell(1, 1)
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetPadding(8f)
                .SetBorder(borde);

            if (!IntentarAgregarLogoEnCelda(celdaDer, new[] { "familias-dif-rosa.png", "familias-dif.png" }, 64f, 48f))
            {
                celdaDer.Add(new Paragraph("Familias").SetFont(bold).SetFontSize(11f).SetFontColor(PdfApiEstiloRequi.TextoPrincipal))
                    .Add(new Paragraph("Sistema Estatal DIF").SetFont(regular).SetFontSize(7f).SetFontColor(PdfApiEstiloRequi.TextoSecundario))
                    .Add(new Paragraph(" ").SetFontSize(4f));
            }
            else
            {
                celdaDer.Add(new Paragraph("Sistema Estatal DIF").SetFont(regular).SetFontSize(6f).SetFontColor(PdfApiEstiloRequi.TextoSecundario)
                    .SetMarginTop(2f));
            }

            tblEnc.AddCell(celdaDer);

            // Segunda fila: la fecha ya no comparte alto con el título (evita franjas vacías bajo EJERCICIO).
            var tblFecha = new Table(1).UseAllAvailableWidth();
            tblFecha.AddCell(new Cell()
                .SetBorder(bordeFecha)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                .SetPadding(6f)
                .SetTextAlignment(TextAlignment.CENTER)
                .Add(new Paragraph("Fecha de elaboración").SetFont(bold).SetFontSize(7f)
                    .SetFontColor(PdfApiEstiloRequi.TextoSecundario))
                .Add(new Paragraph(S(fechaElaboracion)).SetFont(regular).SetFontSize(7.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoPrincipal).SetMarginTop(2f)));

            // Tres columnas iguales: la fecha queda centrada en la página.
            var filaFechaCentrada = new Table(UnitValue.CreatePercentArray(new float[] { 33.34f, 33.33f, 33.33f }))
                .UseAllAvailableWidth();
            filaFechaCentrada.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f));
            filaFechaCentrada.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f).Add(tblFecha));
            filaFechaCentrada.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f));

            tblEnc.AddCell(new Cell(1, 3)
                .SetBackgroundColor(ColorConstants.WHITE)
                .SetPaddingTop(2f)
                .SetPaddingRight(4f)
                .SetPaddingBottom(4f)
                .SetPaddingLeft(4f)
                .SetBorderTop(Border.NO_BORDER)
                .SetBorderLeft(borde)
                .SetBorderRight(borde)
                .SetBorderBottom(borde)
                .Add(filaFechaCentrada));

            return tblEnc;
        }

        private static bool IntentarAgregarLogoEnCelda(Cell celda, string[] nombresArchivo, float maxAnchoPt, float maxAltoPt)
        {
            foreach (var nombre in nombresArchivo)
            {
                var ruta = ResolverRutaImagenWwwRoot(nombre);
                if (ruta == null) continue;
                try
                {
                    var img = new Image(ImageDataFactory.Create(ruta));
                    img.SetHorizontalAlignment(HorizontalAlignment.CENTER);
                    img.ScaleToFit(maxAnchoPt, maxAltoPt);
                    celda.Add(img);
                    return true;
                }
                catch
                {
                }
            }
            return false;
        }

        private static string? ResolverRutaImagenWwwRoot(string nombreArchivo)
        {
            if (string.IsNullOrWhiteSpace(nombreArchivo)) return null;
            foreach (var raiz in EnumerarRaicesPosiblesWeb())
            {
                var ruta = System.IO.Path.Combine(raiz, "wwwroot", "img", nombreArchivo);
                if (File.Exists(ruta)) return System.IO.Path.GetFullPath(ruta);
            }
            return null;
        }

        private static List<string> EnumerarRaicesPosiblesWeb()
        {
            var rutas = new List<string>();
            var vistos = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Agregar(string? dir)
            {
                if (string.IsNullOrWhiteSpace(dir)) return;
                string full;
                try { full = System.IO.Path.GetFullPath(dir); }
                catch { return; }
                if (vistos.Add(full)) rutas.Add(full);
            }

            Agregar(Directory.GetCurrentDirectory());
            var bd = AppContext.BaseDirectory;
            if (!string.IsNullOrEmpty(bd))
            {
                Agregar(bd);
                try
                {
                    var info = new DirectoryInfo(bd);
                    for (int i = 0; i < 8 && info != null; i++, info = info.Parent)
                    {
                        var web = System.IO.Path.Combine(info.FullName, "Inventario.AplicacionWeb");
                        if (Directory.Exists(web)) Agregar(web);
                    }
                }
                catch { /* ignorar rutas inválidas */ }
            }

            return rutas;
        }

        /// <summary>
        /// Normaliza nulls tras el model binding del formulario; no rellena desde BD.
        /// </summary>
        private static void AsegurarTablaApiDesdeFormulario(TablaApiEditableDTO m)
        {
            static string Z(string? s) => s ?? "";
            m.FechaElaboracion = Z(m.FechaElaboracion);
            m.Ejercicio = Z(m.Ejercicio);
            m.AreaSolicitanteClave = Z(m.AreaSolicitanteClave);
            m.AreaSolicitanteNombre = Z(m.AreaSolicitanteNombre);
            m.DescripcionBienServicio = Z(m.DescripcionBienServicio);
            m.Justificacion = Z(m.Justificacion);
            m.NumeroRequisicion = Z(m.NumeroRequisicion);
            m.OficioSuficiencia = Z(m.OficioSuficiencia);
            m.ContratoAsociado = Z(m.ContratoAsociado);
            m.Comentarios = Z(m.Comentarios);
            m.TotalSolicitado = Z(m.TotalSolicitado);
            m.TotalAutorizado = Z(m.TotalAutorizado);
            m.Partidas ??= new List<TablaApiPartidaEditableDTO>();
            foreach (var p in m.Partidas)
            {
                p.Numero = Z(p.Numero);
                p.Ua = Z(p.Ua);
                p.ClaveMunicipio = Z(p.ClaveMunicipio);
                p.ImporteSolicitado = Z(p.ImporteSolicitado);
                p.FuenteFinanciamiento = Z(p.FuenteFinanciamiento);
                p.Pp = Z(p.Pp);
                p.Componente = Z(p.Componente);
                p.Actividad = Z(p.Actividad);
                p.ObjetoGasto = Z(p.ObjetoGasto);
                p.ImporteAutorizado = Z(p.ImporteAutorizado);
            }
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

        private async Task<Dictionary<int, (decimal PrecioUnitario, decimal Cantidad, bool? IVA)>> ObtenerCotizacionConCantidadAsync(int idRequisicion)
        {
            var queryCot = await _repositoryCotizaciones.Consultar(
                c => c.IdRequisicion == idRequisicion
                  && c.IdRequiDetalle.HasValue
                  && c.Importe.HasValue);
            var cotizaciones = await queryCot.ToListAsync();

            var queryDet = await _repoDetalle.Consultar(d => d.IdRequisicion == idRequisicion);
            var detalles = await queryDet.ToListAsync();

            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m);

            // ✅ Ganador = proveedor con menor TOTAL GLOBAL (suma de precio×cantidad×iva por todas sus partidas)
            // Igual que el cuadro comparativo
            var totalPorProveedor = cotizaciones
                .GroupBy(c => c.IdProveedor)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c =>
                    {
                        var cantidad = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var cant) ? cant : 1m;
                        var precio = c.Importe!.Value * cantidad;
                        return c.Iva == true ? precio * 1.16m : precio;
                    }));

            // El proveedor ganador es el de menor total global
            var idProveedorGanador = totalPorProveedor
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Value)
                .Select(kv => kv.Key)
                .FirstOrDefault();

            // Tomar solo las cotizaciones del proveedor ganador, una por partida
            var cotGanadora = cotizaciones
                .Where(c => c.IdProveedor == idProveedorGanador)
                .GroupBy(c => c.IdRequiDetalle!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.First());

            return cotGanadora.ToDictionary(
                kv => kv.Key,
                kv => (
                    PrecioUnitario: kv.Value.Importe!.Value,
                    Cantidad: cantidadPorPartida.TryGetValue(kv.Key, out var cant) ? cant : 1m,
                    IVA: kv.Value.Iva
                ));
        }

        private static decimal AplicarIva(decimal precioUnitario, decimal cantidad) =>
            precioUnitario * cantidad * 1.16m;

        private static string FormatearImporte(decimal valor) =>
            valor.ToString("C2", new System.Globalization.CultureInfo("es-MX"));

        // ════════════════════════════════════════════════════════════════
        // PEDIDO
        // ════════════════════════════════════════════════════════════════

        public async Task<PedidoVistaDTO> ObtenerPedidoEditableAsync(int idRequisicion)
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
            var detalles = await queryDet
                .Select(d => new
                {
                    d.IdRequisicionDetalle,
                    d.Descripcion,
                    d.Cantidad,
                    d.UnidadMedida,
                    Clave = d.IdArticuloNavigation != null ? d.IdArticuloNavigation.Clave : null,
                    ClaveMaterial = d.IdArticuloNavigation != null ? (int?)d.IdArticuloNavigation.ClaveMaterial : null
                })
                .ToListAsync();

            var queryCot = await _repositoryCotizaciones.Consultar(
                c => c.IdRequisicion == idRequisicion && c.IdRequiDetalle.HasValue && c.Importe.HasValue);
            var cotizaciones = await queryCot
                .Select(c => new
                {
                    c.IdProveedor,
                    c.IdRequiDetalle,
                    c.Importe,
                    c.Iva,
                    ProvNombre = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreProvedor : "",
                    ProvDireccion = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Direccion : "",
                    ProvRfc = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Rfc : ""
                })
                .ToListAsync();

            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad ?? 1m);

            var totalPorProveedor = cotizaciones
                .Where(c => c.IdProveedor.HasValue)
                .GroupBy(c => c.IdProveedor!.Value)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(c =>
                    {
                        var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                        var precio = c.Importe!.Value * cant;
                        return c.Iva == true ? precio * 1.16m : precio;
                    }));

            var idGanador = totalPorProveedor
                .Where(kv => kv.Value > 0)
                .OrderBy(kv => kv.Value)
                .Select(kv => (int?)kv.Key)
                .FirstOrDefault();

            var provGanador = idGanador.HasValue
                ? cotizaciones.FirstOrDefault(c => c.IdProveedor == idGanador)
                : null;

            var cotGanadora = cotizaciones
                .Where(c => c.IdProveedor == idGanador && c.IdRequiDetalle.HasValue)
                .GroupBy(c => c.IdRequiDetalle!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var dto = new PedidoVistaDTO
            {
                IdRequisicion = idRequisicion,
                NumRequisicion = requisicion.NumRequisicion ?? "",
                ProveedorNombre = provGanador?.ProvNombre ?? "",
                ProveedorDireccion = provGanador?.ProvDireccion ?? "",
                ProveedorRfc = provGanador?.ProvRfc ?? "",
                Departamento = nombreDepartamento,
                Responsable = requisicion.NomResponsableDepartamento ?? "",
                LugarEntrega = requisicion.LugarEntrega ?? "",
                PartidaPresupuestal = requisicion.IdPp?.ToString() ?? ""
            };

            for (int i = 0; i < detalles.Count; i++)
            {
                var det = detalles[i];
                cotGanadora.TryGetValue(det.IdRequisicionDetalle, out var cot);
                dto.Partidas.Add(new PedidoPartidaVistaDTO
                {
                    Numero = i + 1,
                    Clave = det.Clave ?? det.ClaveMaterial?.ToString() ?? "",
                    Descripcion = det.Descripcion ?? "",
                    Cantidad = det.Cantidad ?? 1m,
                    UnidadMedida = det.UnidadMedida ?? "",
                    PrecioUnitario = cot?.Importe ?? 0m,
                    TieneIva = cot?.Iva ?? false
                });
            }

            // Precalcular totales para que la vista los muestre por defecto
            decimal sumaInicial = dto.Partidas.Sum(p => p.PrecioUnitario * p.Cantidad);
            decimal ivaInicial = sumaInicial * 0.16m;
            decimal subtotalInicial = sumaInicial + ivaInicial;
            decimal retencionInicial = subtotalInicial * 0.005m;
            dto.Suma = sumaInicial;
            dto.Iva = ivaInicial;
            dto.Descuento = 0m;
            dto.Subtotal = subtotalInicial;
            dto.Retencion = retencionInicial;
            dto.Total = subtotalInicial - retencionInicial;

            return dto;
        }

        public async Task<byte[]> GenerarPedidoPdfAsync(PedidoVistaDTO form, string webRootPath)
        {
            var vista = await ObtenerPedidoEditableAsync(form.IdRequisicion);
            vista.NumeroPedido = form.NumeroPedido;
            vista.TiempoEntrega = form.TiempoEntrega;
            vista.CondicionesPago = form.CondicionesPago;

            // Usar totales editados por el usuario
            decimal suma       = form.Suma;
            decimal iva        = form.Iva;
            decimal descuento  = form.Descuento;
            decimal subtotal   = form.Subtotal;
            decimal retencion  = form.Retencion;
            decimal total      = form.Total;

            var MX = new System.Globalization.CultureInfo("es-MX");
            string Fmt(decimal v) => v.ToString("C2", MX);

            var ms = new MemoryStream();
            using var pdfWriter = new PdfWriter(ms);
            using var pdfDoc = new PdfDocument(pdfWriter);
            var doc = new Document(pdfDoc, PageSize.LETTER);
            doc.SetMargins(18f, 22f, 18f, 22f);

            var bold    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var borde     = new SolidBorder(PdfApiEstiloRequi.Borde, 0.85f);
            var bordeGris = new SolidBorder(new DeviceRgb(203, 213, 225), 0.75f);

            // ── helpers ──────────────────────────────────────────────────────
            Cell CHead(string txt, int cs = 1, int rs = 1, TextAlignment al = TextAlignment.LEFT) =>
                new Cell(rs, cs)
                    .SetBorder(borde).SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla)
                    .SetPadding(4f).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(al)
                    .Add(new Paragraph(S(txt)).SetFont(bold).SetFontSize(6.6f)
                        .SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla));

            Cell CVal(string txt, int cs = 1, int rs = 1, TextAlignment al = TextAlignment.LEFT,
                      bool negrita = false, float sz = 6.8f) =>
                new Cell(rs, cs)
                    .SetBorder(borde).SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(al)
                    .Add(new Paragraph(S(txt)).SetFont(negrita ? bold : regular).SetFontSize(sz)
                        .SetFontColor(PdfApiEstiloRequi.TextoPrincipal));

            Cell CVacia(int cs = 1, int rs = 1, float h = 14f) =>
                new Cell(rs, cs).SetHeight(h).SetPadding(0f).SetBorder(borde)
                    .SetBackgroundColor(ColorConstants.WHITE);

            // ── ENCABEZADO INSTITUCIONAL (logos + título) ─────────────────
            var tblHeader = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 64f, 18f }))
                .UseAllAvailableWidth().SetBorder(borde);

            var celdaIzq = new Cell().SetBorder(borde)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER).SetPadding(5f);
            var rutaIzq = System.IO.Path.Combine(webRootPath, "img", "corazon.png");
            if (System.IO.File.Exists(rutaIzq))
                celdaIzq.Add(new Image(ImageDataFactory.Create(rutaIzq)).ScaleToFit(88f, 40f)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                celdaIzq.Add(new Paragraph("PUEBLA").SetFont(bold).SetFontSize(10f));
            tblHeader.AddCell(celdaIzq);

            var celdaCentro = new Cell().SetBorder(borde)
                .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(5f);
            celdaCentro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA DEL ESTADO DE PUEBLA")
                .SetFont(bold).SetFontSize(7f).SetFontColor(PdfApiEstiloRequi.TextoInstitucional));
            celdaCentro.Add(new Paragraph("DIRECCIÓN DE ADMINISTRACIÓN Y FINANZAS")
                .SetFont(bold).SetFontSize(6.5f).SetFontColor(PdfApiEstiloRequi.TextoInstitucional));
            celdaCentro.Add(new Paragraph("DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES")
                .SetFont(bold).SetFontSize(6.5f).SetFontColor(PdfApiEstiloRequi.TextoInstitucional));
            celdaCentro.Add(new Paragraph($"PEDIDO DE COMPRA  Nº: {S(vista.NumeroPedido)}")
                .SetFont(bold).SetFontSize(10f).SetFontColor(PdfApiEstiloRequi.RosaAcento).SetMarginTop(4f));
            tblHeader.AddCell(celdaCentro);

            var celdaDer = new Cell().SetBorder(borde)
                .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER).SetPadding(5f);
            var rutaDer = System.IO.Path.Combine(webRootPath, "img", "familias-dif-rosa.png");
            if (System.IO.File.Exists(rutaDer))
                celdaDer.Add(new Image(ImageDataFactory.Create(rutaDer)).ScaleToFit(92f, 40f)
                    .SetHorizontalAlignment(HorizontalAlignment.CENTER));
            else
                celdaDer.Add(new Paragraph("Familias").SetFont(bold).SetFontSize(10f));
            tblHeader.AddCell(celdaDer);
            doc.Add(tblHeader);
            doc.Add(new Paragraph(" ").SetMarginTop(3f));

            // ── ENCABEZADO DATOS (proveedor / depto) ─────────────────────
            var tblEnc = new Table(UnitValue.CreatePercentArray(new float[] { 22f, 28f, 22f, 28f }))
                .UseAllAvailableWidth();

            tblEnc.AddCell(CHead("PROVEEDOR ADJUDICADO", rs: 2, al: TextAlignment.CENTER));
            tblEnc.AddCell(CVal(vista.ProveedorNombre, rs: 2, al: TextAlignment.CENTER, negrita: true));
            tblEnc.AddCell(CHead("DEPARTAMENTO SOLICITANTE"));
            tblEnc.AddCell(CVal(vista.Departamento, negrita: true));

            tblEnc.AddCell(CHead("RESPONSABLE"));
            tblEnc.AddCell(CVal(vista.Responsable));

            tblEnc.AddCell(CHead("DIRECCIÓN", rs: 2));
            tblEnc.AddCell(CVal(vista.ProveedorDireccion, rs: 2));
            tblEnc.AddCell(CHead("LUGAR DE ENTREGA"));
            tblEnc.AddCell(CVal(vista.LugarEntrega));

            tblEnc.AddCell(CHead("TIEMPO DE ENTREGA"));
            tblEnc.AddCell(CVal(vista.TiempoEntrega));

            tblEnc.AddCell(CHead("R.F.C."));
            tblEnc.AddCell(CVal(vista.ProveedorRfc));
            tblEnc.AddCell(CHead("CONDICIONES DE PAGO"));
            tblEnc.AddCell(CVal(vista.CondicionesPago));
            doc.Add(tblEnc);

            // ── PARTIDA PRESUPUESTAL ──────────────────────────────────────
            var amarillo = new DeviceRgb(255, 250, 200);
            var tblPP = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f })).UseAllAvailableWidth();
            tblPP.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(amarillo).SetPadding(4f)
                .Add(new Paragraph($"PARTIDA PRESUPUESTAL: {S(vista.PartidaPresupuestal)}")
                    .SetFont(bold).SetFontSize(7f).SetFontColor(new DeviceRgb(120, 80, 0))));
            tblPP.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(amarillo).SetPadding(4f)
                .Add(new Paragraph("USO: PARA USO DEL DEPARTAMENTO SOLICITANTE.")
                    .SetFont(regular).SetFontSize(7f).SetFontColor(new DeviceRgb(120, 80, 0))));
            doc.Add(tblPP);
            doc.Add(new Paragraph(" ").SetMarginTop(2f));

            // ── TABLA DE ARTÍCULOS ────────────────────────────────────────
            var tblArt = new Table(UnitValue.CreatePercentArray(new float[] { 6f, 10f, 38f, 9f, 9f, 14f, 14f }))
                .UseAllAvailableWidth();

            foreach (var h in new[] { "No.", "CLAVE", "DESCRIPCIÓN", "CANTIDAD", "UNIDAD", "PRECIO UNITARIO", "PRECIO TOTAL" })
                tblArt.AddHeaderCell(new Cell().SetBorder(borde)
                    .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.CENTER)
                    .Add(new Paragraph(h).SetFont(bold).SetFontSize(6.2f)
                        .SetFontColor(PdfApiEstiloRequi.TextoEncabezadoTabla)));

            int minFilas = Math.Max(10, vista.Partidas.Count + 2);
            for (int i = 0; i < minFilas; i++)
            {
                var bg = (i % 2 == 1) ? PdfApiEstiloRequi.FondoEncabezadoTabla : ColorConstants.WHITE;
                if (i < vista.Partidas.Count)
                {
                    var p = vista.Partidas[i];
                    decimal precioTotal = p.PrecioUnitario * p.Cantidad;
                    tblArt.AddCell(CVal(p.Numero.ToString(), al: TextAlignment.CENTER));
                    tblArt.AddCell(CVal(p.Clave));
                    tblArt.AddCell(CVal(p.Descripcion));
                    tblArt.AddCell(CVal(p.Cantidad.ToString("N0"), al: TextAlignment.CENTER));
                    tblArt.AddCell(CVal(p.UnidadMedida, al: TextAlignment.CENTER));
                    tblArt.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(bg).SetPadding(4f)
                        .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .Add(new Paragraph(Fmt(p.PrecioUnitario)).SetFont(regular).SetFontSize(6.8f)
                            .SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));
                    tblArt.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(bg).SetPadding(4f)
                        .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                        .Add(new Paragraph(Fmt(precioTotal)).SetFont(bold).SetFontSize(6.8f)
                            .SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));
                }
                else
                {
                    for (int c = 0; c < 6; c++) tblArt.AddCell(CVacia());
                    tblArt.AddCell(new Cell().SetHeight(14f).SetBorder(borde).SetBackgroundColor(bg).SetPadding(4f)
                        .SetTextAlignment(TextAlignment.RIGHT)
                        .Add(new Paragraph("").SetFont(regular).SetFontSize(6.8f)));
                }
            }
            doc.Add(tblArt);

            // ── TOTALES ───────────────────────────────────────────────────
            var tblTot = new Table(UnitValue.CreatePercentArray(new float[] { 70f, 18f, 12f }))
                .UseAllAvailableWidth();

            // Celda vacía izquierda que ocupa todas las filas de totales
            tblTot.AddCell(new Cell(6, 1).SetBorder(borde).SetBackgroundColor(ColorConstants.WHITE));

            var totalesFilas = new (string Label, string Valor, bool esTotal)[]
            {
                ("SUMA",             Fmt(suma),      false),
                ("I.V.A. 16%",       Fmt(iva),       false),
                ("DESCUENTO",        Fmt(descuento), false),
                ("SUBTOTAL",         Fmt(subtotal),  false),
                ("RET. 5 AL MILLAR", Fmt(retencion), false),
                ("TOTAL",            Fmt(total),     true)
            };

            foreach (var (lbl, val, esTotal) in totalesFilas)
            {
                var bgTotal = esTotal ? PdfApiEstiloRequi.RosaAcento : PdfApiEstiloRequi.FondoEncabezadoTabla;
                var fgTotal = esTotal ? ColorConstants.WHITE : PdfApiEstiloRequi.TextoEncabezadoTabla;
                var bgVal   = esTotal ? new DeviceRgb(255, 235, 240) : ColorConstants.WHITE;
                tblTot.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(bgTotal).SetPadding(4f)
                    .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .Add(new Paragraph(lbl).SetFont(bold).SetFontSize(6.6f).SetFontColor(fgTotal)));
                tblTot.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(bgVal).SetPadding(4f)
                    .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .Add(new Paragraph(val).SetFont(bold).SetFontSize(7f)
                        .SetFontColor(esTotal ? PdfApiEstiloRequi.RosaAcento : PdfApiEstiloRequi.TextoPrincipal)));
            }
            doc.Add(tblTot);
            doc.Add(new Paragraph(" ").SetMarginTop(4f));

            // ── FIRMAS ────────────────────────────────────────────────────
            var tblFirmas = new Table(UnitValue.CreatePercentArray(new float[] { 25f, 25f, 25f, 25f }))
                .UseAllAvailableWidth();
            var firmantes = new (string Cargo, string Nombre)[]
            {
                ("JEFE DE SECCIÓN DE ADQUISICIONES", "C. ROGER ROJAS PÉREZ"),
                ("JEFA DE DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES", "C. MARIA GABRIELA OLIVARES ROBLES"),
                ("DIRECTOR DE ADMINISTRACIÓN Y FINANZAS", "C. MARCOS MATAMOROS MORENO"),
                ("RECIBÍ ORIGINAL", "PROVEEDOR")
            };
            foreach (var (cargo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell().SetBorder(borde).SetMinHeight(70f)
                    .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.TOP).SetPadding(5f)
                    .Add(new Paragraph(cargo).SetFont(bold).SetFontSize(5.8f)
                        .SetFontColor(PdfApiEstiloRequi.TextoSecundario))
                    .Add(new Paragraph("\n\n\n").SetFontSize(8f))
                    .Add(new Paragraph(nombre).SetFont(bold).SetFontSize(6.2f)
                        .SetFontColor(PdfApiEstiloRequi.TextoPrincipal)));
            }
            doc.Add(tblFirmas);

            // ── PIE DE PÁGINA ─────────────────────────────────────────────
            doc.Add(new Paragraph(" ").SetMarginTop(4f));
            var tblPie = new Table(UnitValue.CreatePercentArray(new float[] { 65f, 35f }))
                .UseAllAvailableWidth();
            var condiciones = new[]
            {
                "1. ENTREGAR LOS MATERIALES CONTENIDOS EN ESTE PEDIDO, DIRECTAMENTE AL ALMACÉN DIF SALVO INSTRUCCIONES EN CONTRARIO CON REMISIÓN/FACTURA EN 3 EJEMPLARES ENTREGANDO UNA COPIA EN LA SECCIÓN DE ADQUISICIONES.",
                "2. PRESENTAR A REVISIÓN EN EL DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES, FACTURA ORIGINAL DEL PEDIDO Y SELLADO POR EL SOLICITANTE.",
                "3. LOS PEDIDOS DEBEN ENTREGARSE A ENTERA SATISFACCIÓN DEL SOLICITANTE."
            };
            var parrafo = new Paragraph();
            foreach (var c in condiciones)
                parrafo.Add(new Text(c + "\n").SetFont(regular).SetFontSize(5.5f)
                    .SetFontColor(PdfApiEstiloRequi.TextoPrincipal));
            tblPie.AddCell(new Cell().SetBorder(borde).SetPadding(5f).Add(parrafo));

            var dirParrafo = new Paragraph()
                .Add(new Text("AV. REFORMA 1305 • CENTRO HISTÓRICO\nPUEBLA, PUE. C.P. 72000\nTEL: (222) 229-5200\n")
                    .SetFont(regular).SetFontSize(6f).SetFontColor(new DeviceRgb(0, 70, 180)))
                .Add(new Text("PEDIDO ORIGINAL").SetFont(bold).SetFontSize(7.5f)
                    .SetFontColor(PdfApiEstiloRequi.RosaAcento));
            tblPie.AddCell(new Cell().SetBorder(borde).SetPadding(6f)
                .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                .Add(dirParrafo));
            doc.Add(tblPie);

            doc.Close();
            return ms.ToArray();
        }
    }
}
