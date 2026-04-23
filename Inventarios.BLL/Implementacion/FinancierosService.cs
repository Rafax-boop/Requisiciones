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
        private readonly IGenericRepository<TblRequisicionDetalleMovimiento> _repoMovimiento;

        public FinancierosService(
            IRequisicionRepository repositoryRequisicion,
            IGenericRepository<TblBitacoraEstatus> repositoryBitacora,
            IGenericRepository<TblRegistroDiseno> repositoryDiseno,
            IGenericRepository<TblDepartamento> repoDepartamento,
            IGenericRepository<TblRequisicionDetalle> repoDetalle,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblTablaApiHistorial> repoHistorial,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDiseno = repositoryDiseno;
            _repoDepartamento = repoDepartamento;
            _repoDetalle = repoDetalle;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repoHistorial = repoHistorial;
            _repoMovimiento = repoMovimiento;
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

            var detalles = await ObtenerDetallesFiltradosAsync(idRequisicion, requisicion.RequiServicio ?? false);
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

            var detalles = await ObtenerDetallesFiltradosAsync(idRequisicion, requisicion.RequiServicio ?? false);

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

        private async Task<List<TblRequisicionDetalle>> ObtenerDetallesFiltradosAsync(int idRequisicion, bool esServicio)
        {
            var query = await _repoDetalle.Consultar(d => d.IdRequisicion == idRequisicion);

            if (!esServicio)
            {
                var queryMovimientos = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion && m.TipoMovimiento == "COMPRA");
                var idsParaCompra = await queryMovimientos
                    .Select(m => m.IdRequisicionDetalle)
                    .Distinct()
                    .ToListAsync();

                if (idsParaCompra.Any())
                    query = query.Where(r => idsParaCompra.Contains(r.IdRequisicionDetalle));
            }

            return await query.ToListAsync();
        }
    }
}
