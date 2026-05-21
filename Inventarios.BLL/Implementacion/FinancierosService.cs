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
        private readonly IGenericRepository<TblProveedorGanador> _repositoryGanador;
        private readonly IGenericRepository<TblRequisicionDetalleMunicipio> _repoMunicipiosDetalle;
        private readonly IGenericRepository<TblConsolidada> _repoConsolidada;
        private readonly IGenericRepository<TblConsolidadasDetalle> _repoConsolidadaDetalle;
        private readonly IGenericRepository<TblApiPartida> _repoApiPartidas;
        private readonly IGenericRepository<TblFuentesFinanciamiento> _repoFuentes;

        public FinancierosService(
            IRequisicionRepository repositoryRequisicion,
            IGenericRepository<TblBitacoraEstatus> repositoryBitacora,
            IGenericRepository<TblRegistroDiseno> repositoryDiseno,
            IGenericRepository<TblDepartamento> repoDepartamento,
            IGenericRepository<TblRequisicionDetalle> repoDetalle,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblTablaApiHistorial> repoHistorial,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento,
            IGenericRepository<TblProveedorGanador> repositoryGanador,
            IGenericRepository<TblRequisicionDetalleMunicipio> repoMunicipiosDetalle,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repoConsolidadaDetalle,
            IGenericRepository<TblApiPartida> repoApiPartidas,
            IGenericRepository<TblFuentesFinanciamiento> repoFuentes)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDiseno = repositoryDiseno;
            _repoDepartamento = repoDepartamento;
            _repoDetalle = repoDetalle;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repoHistorial = repoHistorial;
            _repoMovimiento = repoMovimiento;
            _repositoryGanador = repositoryGanador;
            _repoMunicipiosDetalle = repoMunicipiosDetalle;
            _repoConsolidada = repoConsolidada;
            _repoConsolidadaDetalle = repoConsolidadaDetalle;
            _repoApiPartidas = repoApiPartidas;
            _repoFuentes = repoFuentes;
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
                .Where(r => (r.IdUsuarioFinan.HasValue || r.IdEstatus == 13) && r.ConsolidadaId == null)
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

        public async Task<List<ConsolidadaFinancierosDTO>> ListarConsolidadasFinancieros(int? idUsuario = null, List<int>? estatusPermitidos = null)
        {
            var query = await _repoConsolidada.Consultar();

            if (idUsuario.HasValue)
                query = query.Where(c => c.IdUsuarioFinan == idUsuario.Value);

            if (estatusPermitidos == null || estatusPermitidos.Count == 0)
            {
                // Por defecto: traer todas las consolidadas asignadas a un analista financiero
                // o que esten en estatus 13 (listas para asignar). Esto evita que desaparezcan
                // al cambiar de estatus (14, 15, 17, etc.) mientras tengan IdUsuarioFinan.
                query = query.Where(c => c.IdUsuarioFinan.HasValue || c.IdEstatus == 13);
                query = query.Where(c => c.IdEstatus != 7 && c.IdEstatus != 5 && c.IdEstatus != 12);
            }
            else
            {
                query = query.Where(c => estatusPermitidos.Contains(c.IdEstatus));
            }

            var consolidaciones = await query
                .Include(c => c.IdEstatusNavigation)
                .Include(c => c.IdUsuarioFinanNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                .OrderBy(c => c.FechaCreacion)
                .ThenBy(c => c.ConsolidadaId)
                .ToListAsync();

            if (consolidaciones.Count == 0) return new List<ConsolidadaFinancierosDTO>();

            var ids = consolidaciones.Select(c => c.ConsolidadaId).ToList();

            var queryDeptos = await _repoConsolidadaDetalle.Consultar(d => ids.Contains(d.ConsolidadaId));
            var deptosPorConsolidada = await queryDeptos
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .ToListAsync();

            var deptosAgrupados = deptosPorConsolidada
                .GroupBy(d => d.ConsolidadaId)
                .ToDictionary(g => g.Key, g => string.Join(", ",
                    g.Select(x => x.IdRequisicionNavigation?.IdDepartamentoNavigation?.NombreDepartamento)
                      .Where(n => n != null)
                      .Distinct()));

            var ahora = DateTime.Now;
            var resultado = new List<ConsolidadaFinancierosDTO>(consolidaciones.Count);
            foreach (var c in consolidaciones)
            {
                resultado.Add(new ConsolidadaFinancierosDTO
                {
                    ConsolidadaId = c.ConsolidadaId,
                    FolioConsolidada = c.FolioConsolidada,
                    CantidadRequis = c.TblConsolidadasDetalles?.Count ?? 0,
                    Departamentos = deptosAgrupados.TryGetValue(c.ConsolidadaId, out var deptos) ? deptos : "",
                    FechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy"),
                    IdEstatus = c.IdEstatus,
                    Estatus = c.IdEstatusNavigation?.NombreEstatus ?? "",
                    DiasAsignado = c.IdUsuarioFinan != null && c.FechaModificacion.HasValue
                        ? (int)(ahora - c.FechaModificacion.Value).TotalDays
                        : 0,
                    NombreAsignado = c.IdUsuarioFinanNavigation?.Usuario,
                    IdUsuarioFinan = c.IdUsuarioFinan
                });
            }

            return resultado;
        }

        public async Task<bool> AsignarConsolidada(int idConsolidada, int idUsuarioLog, int idUsuarioFinan)
        {
            try
            {
                var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
                if (consolidada == null) return false;

                consolidada.IdUsuarioFinan = idUsuarioFinan;
                consolidada.IdEstatus = 14;
                consolidada.FechaModificacion = DateTime.Now;
                await _repoConsolidada.Editar(consolidada);

                var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
                var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

                foreach (var hija in hijas)
                {
                    var bitacora = new TblBitacoraEstatus
                    {
                        IdRequisicion = hija.IdRequisicion,
                        IdEstatus = 14,
                        FechaEstatus = DateTime.Now,
                        Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] AsignarFinancieros",
                        IdUsuario = idUsuarioLog
                    };
                    await _repositoryBitacora.Crear(bitacora);

                    hija.IdEstatus = 14;
                    hija.IdUsuarioFinan = idUsuarioFinan;
                    hija.FechaModificacion = DateTime.Now;
                    await _repositoryRequisicion.Editar(hija);
                }

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<AtenderResultadoDTO> AtenderConsolidadaFinancieros(AtenderConsolidadaDTO modelo, int idUsuario)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == modelo.IdConsolidada);
            if (consolidada == null) return new AtenderResultadoDTO { Exito = false };

            var numPedido = await GenerarNumeroPedidoAsync();

            consolidada.IdEstatus = 15;
            consolidada.NumPedido = numPedido;
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == modelo.IdConsolidada);
            var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

            foreach (var hija in hijas)
            {
                hija.IdEstatus = 15;
                hija.NumPedido = numPedido;
                hija.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(hija);

                await _repositoryBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = hija.IdRequisicion,
                    IdEstatus = 15,
                    FechaEstatus = DateTime.Now,
                    Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] Autorizada — {modelo.Observaciones}",
                    IdUsuario = idUsuario
                });
            }

            await GuardarArchivosConsolidada(modelo.DocSiaf, modelo.IdConsolidada, "SIAF", "DocumentoSIAF");
            await GuardarArchivosConsolidada(modelo.TablaApi, modelo.IdConsolidada, "TablaApi", "TablaApi");

            return new AtenderResultadoDTO { Exito = true, NumPedido = numPedido };
        }

        public async Task<(bool Success, string Message)> EnviarFinancierosConsolidadaAsync(int idConsolidada, int idUsuario, IFormFile? archivo = null, string? webRootPath = null)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null)
                return (false, "La consolidada no existe.");

            if (consolidada.IdEstatus != 15 && consolidada.IdEstatus != 16 && consolidada.IdEstatus != 18)
                return (false, $"La consolidada debe estar en estatus de verificación (actual: {consolidada.IdEstatus}).");

            if (archivo != null && archivo.Length > 0 && !string.IsNullOrEmpty(webRootPath))
            {
                var carpeta = $"consolidada_{idConsolidada}";
                var rutaBase = System.IO.Path.Combine(webRootPath, "uploads", "PedidoCompra", carpeta);
                System.IO.Directory.CreateDirectory(rutaBase);

                var nombre = $"{Guid.NewGuid()}{System.IO.Path.GetExtension(archivo.FileName)}";
                using (var stream = new System.IO.FileStream(System.IO.Path.Combine(rutaBase, nombre), System.IO.FileMode.Create))
                    await archivo.CopyToAsync(stream);

                await _repositoryDiseno.Crear(new TblRegistroDiseno
                {
                    IdConsolidada = idConsolidada,
                    Ruta = $"/uploads/PedidoCompra/{carpeta}/{nombre}",
                    FechaSubida = DateTime.Now,
                    Tipo = "pedido_compra"
                });
            }

            consolidada.IdEstatus = 17;
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

            foreach (var hija in hijas)
            {
                hija.IdEstatus = 17;
                hija.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(hija);

                await _repositoryBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = hija.IdRequisicion,
                    IdEstatus = 17,
                    FechaEstatus = DateTime.Now,
                    Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] Documentos del proveedor enviados a revisión",
                    IdUsuario = idUsuario
                });
            }

            return (true, "Documentos enviados a financieros correctamente.");
        }

        public async Task<bool> FinalizarRequisicionConsolidada(int idConsolidada, List<IFormFile>? transferencias, int idUsuario)
        {
            try
            {
                var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
                if (consolidada == null) return false;

                consolidada.IdEstatus = 7;
                consolidada.FechaModificacion = DateTime.Now;
                await _repoConsolidada.Editar(consolidada);

                var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
                var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

                foreach (var hija in hijas)
                {
                    hija.IdEstatus = 7;
                    hija.FechaModificacion = DateTime.Now;
                    await _repositoryRequisicion.Editar(hija);

                    await _repositoryBitacora.Crear(new TblBitacoraEstatus
                    {
                        IdRequisicion = hija.IdRequisicion,
                        IdEstatus = 7,
                        FechaEstatus = DateTime.Now,
                        Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] Pago finalizado",
                        IdUsuario = idUsuario
                    });
                }

                if (transferencias != null && transferencias.Any())
                {
                    var carpetaDest = $"consolidada_{idConsolidada}";
                    var rutaBase = System.IO.Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "uploads", "Transferencias", carpetaDest);
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
                            IdConsolidada = idConsolidada,
                            Ruta = $"/uploads/Transferencias/{carpetaDest}/{nombreArchivo}",
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

        public async Task<bool> RebotarDocumentosConsolidada(int idConsolidada, string observaciones,
            List<string> docsObservados, int idUsuario)
        {
            try
            {
                var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
                if (consolidada == null) return false;

                consolidada.IdEstatus = 18;
                consolidada.FechaModificacion = DateTime.Now;
                await _repoConsolidada.Editar(consolidada);

                var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
                var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

                var notaCompleta = $"DOCUMENTOS OBSERVADOS: {string.Join(", ", docsObservados)}. NOTA: {observaciones}";

                foreach (var hija in hijas)
                {
                    hija.IdEstatus = 18;
                    hija.FechaModificacion = DateTime.Now;
                    await _repositoryRequisicion.Editar(hija);

                    await _repositoryBitacora.Crear(new TblBitacoraEstatus
                    {
                        IdRequisicion = hija.IdRequisicion,
                        IdEstatus = 18,
                        FechaEstatus = DateTime.Now,
                        Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] {notaCompleta}",
                        IdUsuario = idUsuario
                    });
                }

                return true;
            }
            catch { throw; }
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

        public async Task<AtenderResultadoDTO> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario)
        {
            var requisicion = await _repositoryRequisicion
                .Obtener(r => r.IdRequisicion == modelo.IdRequisicion);
            if (requisicion == null) return new AtenderResultadoDTO { Exito = false };

            var numPedido = await GenerarNumeroPedidoAsync();

            requisicion.IdEstatus = 15;
            requisicion.FechaModificacion = DateTime.Now;
            requisicion.NumPedido = numPedido;

            await _repositoryRequisicion.Editar(requisicion);

            await _repositoryBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = 15,
                FechaEstatus = DateTime.Now,
                Observacion = modelo.Observaciones,
                IdUsuario = idUsuario
            });

            await GuardarArchivos(modelo.DocSiaf, modelo.IdRequisicion, "SIAF", "DocumentoSIAF");
            await GuardarArchivos(modelo.TablaApi, modelo.IdRequisicion, "TablaApi", "TablaApi");

            return new AtenderResultadoDTO { Exito = true, NumPedido = numPedido };
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
                    var (idRequiDest, idConsolDest, carpetaDest) = await ResolverDestino(idRequisicion);
                    var rutaBase = System.IO.Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot", "uploads", "Transferencias", carpetaDest);
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
                            IdRequisicion = idRequiDest,
                            IdConsolidada = idConsolDest,
                            Ruta = $"/uploads/Transferencias/{carpetaDest}/{nombreArchivo}",
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

        private async Task<(int? IdRequi, int? IdConsolidada, string Carpeta)> ResolverDestino(int idRequisicion)
        {
            var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (req?.ConsolidadaId != null)
                return (null, req.ConsolidadaId, $"consolidada_{req.ConsolidadaId}");
            return (idRequisicion, null, idRequisicion.ToString());
        }

        private async Task GuardarArchivos(
            List<IFormFile>? archivos,
            int idRequisicion,
            string tipo,
            string carpeta)
        {
            if (archivos == null || !archivos.Any()) return;

            var (idRequiDest, idConsolDest, carpetaDest) = await ResolverDestino(idRequisicion);

            var rutaBase = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", carpeta, carpetaDest);

            Directory.CreateDirectory(rutaBase);

            foreach (var archivo in archivos)
            {
                if (archivo.Length == 0) continue;

                var nombreArchivo = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(archivo.FileName)}";
                var rutaFisica = System.IO.Path.Combine(rutaBase, nombreArchivo);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    await archivo.CopyToAsync(stream);

                var rutaBd = $"/uploads/{carpeta}/{carpetaDest}/{nombreArchivo}";

                var registro = new TblRegistroDiseno
                {
                    IdRequisicion = idRequiDest,
                    IdConsolidada = idConsolDest,
                    Ruta = rutaBd,
                    FechaSubida = DateTime.Now,
                    Tipo = tipo
                };

                await _repositoryDiseno.Crear(registro);
            }
        }

        private async Task GuardarArchivosConsolidada(
            List<IFormFile>? archivos,
            int idConsolidada,
            string tipo,
            string carpeta)
        {
            if (archivos == null || !archivos.Any()) return;

            var carpetaDest = $"consolidada_{idConsolidada}";
            var rutaBase = System.IO.Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot", "uploads", carpeta, carpetaDest);

            Directory.CreateDirectory(rutaBase);

            foreach (var archivo in archivos)
            {
                if (archivo.Length == 0) continue;

                var nombreArchivo = $"{Guid.NewGuid()}_{System.IO.Path.GetFileName(archivo.FileName)}";
                var rutaFisica = System.IO.Path.Combine(rutaBase, nombreArchivo);

                using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    await archivo.CopyToAsync(stream);

                var rutaBd = $"/uploads/{carpeta}/{carpetaDest}/{nombreArchivo}";

                var registro = new TblRegistroDiseno
                {
                    IdConsolidada = idConsolidada,
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
                    tblPart.AddCell(CeldaBlanca("", fondoFila: bg));  // Region — sin municipio aquí
                    tblPart.AddCell(CeldaBlanca("", fondoFila: bg));  // ClaveMunicipio
                    tblPart.AddCell(CeldaBlanca(importeStr, fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.Ff), fondoFila: bg));
                    tblPart.AddCell(CeldaBlanca(S(requisicion.IdPp?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                    tblPart.AddCell(CeldaBlanca(S(d.CogEditable?.ToString() ?? d.NumPartida?.ToString()), fondoFila: bg));
                    tblPart.AddCell(CeldaVacia(11f, bg));
                }
                else
                {
                    for (int c = 0; c < 11; c++)
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

            // Cargar municipios por partida para esta requisición
            var queryMunis = await _repoMunicipiosDetalle.Consultar(
                m => m.IdRequisicion == idRequisicion);
            var municipiosPorPartida = await queryMunis
                .Include(m => m.IdMunicipioNavigation)
                .GroupBy(m => m.IdRequisicionDetalle)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.ToList());

            foreach (var d in detalles)
            {
                var importeTotal = importePorPartida.TryGetValue(d.IdRequisicionDetalle, out var imp) ? imp : 0m;
                var cantidadTotalDetalle = d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m;

                municipiosPorPartida.TryGetValue(d.IdRequisicionDetalle, out var municipios);
                var listaMunicipios = (municipios != null && municipios.Any())
                    ? municipios
                    : new List<TblRequisicionDetalleMunicipio>
                      {
              new TblRequisicionDetalleMunicipio
              {
                  IdMunicipio = 0,
                  Cantidad = cantidadTotalDetalle,
                  IdMunicipioNavigation = null
              }
                      };

                foreach (var muni in listaMunicipios)
                {
                    var proporcion = cantidadTotalDetalle > 0
                        ? muni.Cantidad / cantidadTotalDetalle
                        : 0m;
                    var importeMuni = importeTotal * proporcion;

                    modelo.Partidas.Add(new TablaApiPartidaEditableDTO
                    {
                        Numero = (modelo.Partidas.Count + 1).ToString(),
                        Ua = S(requisicion.IdDepartamento?.ToString()),
                        Region = S(muni.IdMunicipioNavigation?.ClaveRegion),
                        ClaveMunicipio = muni.IdMunicipio > 0 ? muni.IdMunicipio.ToString() : "",
                        ImporteSolicitado = importeMuni > 0 ? FormatearImporte(importeMuni) : "",
                        FuenteFinanciamiento = S(requisicion.Ff),
                        Pp = S(requisicion.IdPp?.ToString()),
                        Componente = "",
                        Actividad = "",
                        ObjetoGasto = S(d.CogEditable?.ToString() ?? d.NumPartida?.ToString()),
                        ImporteAutorizado = ""
                    });
                }
            }

            return modelo;
        }

        public async Task<TablaApiEditableDTO> ObtenerTablaApiEditableConsolidadaAsync(int idConsolidada)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada)
                ?? throw new Exception($"No se encontró la consolidada {idConsolidada}.");

            var queryDetConsol = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var hijas = await queryDetConsol
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Select(d => d.IdRequisicionNavigation)
                .Distinct()
                .ToListAsync();

            var idsHijas = hijas.Select(h => h.IdRequisicion).ToList();

            var detallesPorHija = new Dictionary<int, List<TblRequisicionDetalle>>();
            var cotizacionesPorHija = new Dictionary<int, Dictionary<int, (decimal PrecioUnitario, decimal Cantidad, bool? IVA)>>();

            foreach (var hija in hijas)
            {
                var idH = hija.IdRequisicion;
                detallesPorHija[idH] = await ObtenerDetallesFiltradosAsync(idH, hija.RequiServicio ?? false);
                cotizacionesPorHija[idH] = await ObtenerCotizacionConCantidadAsync(idH);
            }

            var queryMunis = await _repoMunicipiosDetalle.Consultar(m => idsHijas.Contains(m.IdRequisicion));
            var municipiosPorDetalle = await queryMunis
                .Include(m => m.IdMunicipioNavigation)
                .GroupBy(m => m.IdRequisicionDetalle)
                .ToDictionaryAsync(g => g.Key, g => g.ToList());

            var grupos = new Dictionary<(int? IdArticulo, int? IdDepartamento, int IdMunicipio),
                (decimal Cantidad, decimal ImporteTotal, int? Cog, string? Region)>();

            foreach (var hija in hijas)
            {
                var idH = hija.IdRequisicion;
                var detalles = detallesPorHija[idH];
                var cotizaciones = cotizacionesPorHija[idH];
                var idDepto = hija.IdDepartamento;

                foreach (var d in detalles)
                {
                    if (!cotizaciones.TryGetValue(d.IdRequisicionDetalle, out var cot))
                        continue;

                    var cantDetalle = d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m;
                    var importeBase = cot.PrecioUnitario * cantDetalle;
                    if (cot.IVA == true) importeBase *= 1.16m;

                    municipiosPorDetalle.TryGetValue(d.IdRequisicionDetalle, out var municipios);

                    if (municipios is { Count: > 0 })
                    {
                        var sumaCant = municipios.Sum(m => m.Cantidad);
                        foreach (var muni in municipios)
                        {
                            var prop = sumaCant > 0 ? muni.Cantidad / sumaCant : 1m;
                            var key = (d.IdArticulo, idDepto, muni.IdMunicipio);

                            if (grupos.TryGetValue(key, out var ex))
                            {
                                grupos[key] = (
                                    ex.Cantidad + cantDetalle * prop,
                                    ex.ImporteTotal + importeBase * prop,
                                    ex.Cog ?? d.CogEditable ?? d.NumPartida,
                                    ex.Region ?? muni.IdMunicipioNavigation?.ClaveRegion
                                );
                            }
                            else
                            {
                                grupos[key] = (
                                    cantDetalle * prop,
                                    importeBase * prop,
                                    d.CogEditable ?? d.NumPartida,
                                    muni.IdMunicipioNavigation?.ClaveRegion
                                );
                            }
                        }
                    }
                    else
                    {
                        var key = (d.IdArticulo, idDepto, 0);
                        if (grupos.TryGetValue(key, out var ex))
                        {
                            grupos[key] = (
                                ex.Cantidad + cantDetalle,
                                ex.ImporteTotal + importeBase,
                                ex.Cog ?? d.CogEditable ?? d.NumPartida,
                                ex.Region
                            );
                        }
                        else
                        {
                            grupos[key] = (cantDetalle, importeBase, d.CogEditable ?? d.NumPartida, null);
                        }
                    }
                }
            }

            var deptoDefault = await _repoDepartamento.Obtener(d => d.IdDepartamento == 19);
            var areaClave = "19";
            var areaNombre = deptoDefault?.NombreDepartamento ?? "DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES";

            var fhoy = DateTime.Now;
            var totalSolicitado = grupos.Values.Sum(g => g.ImporteTotal);
            var primerHija = hijas.FirstOrDefault();

            var modelo = new TablaApiEditableDTO
            {
                IdConsolidada = idConsolidada,
                IdRequisicion = 0,
                FechaElaboracion = fhoy.ToString("dddd, d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-MX")),
                Ejercicio = fhoy.Year.ToString(),
                AreaSolicitanteClave = areaClave,
                AreaSolicitanteNombre = areaNombre,
                DescripcionBienServicio = S(primerHija?.UsoEspecifico),
                Justificacion = S(primerHija?.Justificacion),
                NumeroRequisicion = S(consolidada.FolioConsolidada),
                OficioSuficiencia = "",
                ContratoAsociado = "",
                Comentarios = "",
                TotalSolicitado = totalSolicitado > 0 ? FormatearImporte(totalSolicitado) : "$",
                TotalAutorizado = "$"
            };

            foreach (var (key, val) in grupos
                .OrderBy(g => g.Key.IdDepartamento)
                .ThenBy(g => g.Value.Region)
                .ThenBy(g => g.Key.IdMunicipio)
                .ThenBy(g => g.Key.IdArticulo))
            {
                modelo.Partidas.Add(new TablaApiPartidaEditableDTO
                {
                    Numero = (modelo.Partidas.Count + 1).ToString(),
                    Ua = key.IdDepartamento?.ToString() ?? areaClave,
                    Region = S(val.Region),
                    ClaveMunicipio = key.IdMunicipio > 0 ? key.IdMunicipio.ToString() : "",
                    ImporteSolicitado = val.ImporteTotal > 0 ? FormatearImporte(val.ImporteTotal) : "",
                    FuenteFinanciamiento = S(consolidada.Ff),
                    Pp = S(consolidada.IdPp?.ToString()),
                    Componente = "",
                    Actividad = "",
                    ObjetoGasto = S(val.Cog?.ToString()),
                    ImporteAutorizado = ""
                });
            }

            return modelo;
        }

        public async Task<byte[]> GenerarTablaApiAsync(TablaApiEditableDTO modelo)
        {
            if (modelo.IdRequisicion <= 0 && (modelo.IdConsolidada == null || modelo.IdConsolidada <= 0))
                throw new ArgumentException("La requisición o consolidada es requerida.", nameof(modelo));

            if (modelo.IdRequisicion > 0)
            {
                var requi = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == modelo.IdRequisicion);
                if (requi == null)
                    throw new Exception($"No se encontró la requisición {modelo.IdRequisicion}.");
            }

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

            var colWidths = new float[] { 18f, 24f, 32f, 32f, 48f, 46f, 28f, 38f, 34f, 38f, 46f };
            var tblPart = new Table(UnitValue.CreatePointArray(colWidths)).UseAllAvailableWidth();
            tblPart.AddHeaderCell(new Cell(1, 5)
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
            var colTitles = new[] { "No.", "UA", "REGIÓN", "CLAVE\nMUNICIPIO", "IMPORTE\nSOLICITADO", "FUENTE DE\nFINANCIAMIENTO", "PP", "COMPONENTE", "ACTIVIDAD", "OBJETO\nDEL GASTO", "IMPORTE\nAUTORIZADO" };
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
                    tblPart.AddCell(CeldaBlanca(d.Region, fondoFila: bg));
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
                    for (int c = 0; c < 11; c++) tblPart.AddCell(CeldaVacia(10f, bg));
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
                IdRequisicion = modelo.IdRequisicion > 0 ? modelo.IdRequisicion : null,
                IdConsolidada = modelo.IdConsolidada.GetValueOrDefault() > 0 ? modelo.IdConsolidada : null,
                IdUsuario = idUsuario,
                FechaGeneracion = DateTime.Now,
                DatosJson = json,
                Observacion = observacion
            };

            await _repoHistorial.Crear(registro);
            await PoblarApiPartidasAsync(registro.IdHistorial, modelo);
        }

        public async Task<List<TablaApiHistorialDTO>> ObtenerHistorialTablaApiAsync(int idRequisicion)
        {
            var query = await _repoHistorial.Consultar(
                h => h.IdRequisicion == idRequisicion
                  && (h.Observacion == null || !h.Observacion.StartsWith("Pedido")));

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

            var opciones = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

            return lista.Select(h => new TablaApiHistorialDTO
            {
                IdHistorial = h.IdHistorial,
                IdRequisicion = h.IdRequisicion ?? 0,
                FechaGeneracion = h.FechaGeneracion,
                NombreUsuario = h.NombreUsuario,
                Observacion = h.Observacion,
                Modelo = System.Text.Json.JsonSerializer
                        .Deserialize<TablaApiEditableDTO>(h.DatosJson, opciones)
            }).ToList();
        }

        public async Task<List<TablaApiHistorialDTO>> ObtenerHistorialTablaApiPorConsolidadaAsync(int idConsolidada)
        {
            var query = await _repoHistorial.Consultar(
                h => h.IdConsolidada == idConsolidada
                  && (h.Observacion == null || !h.Observacion.StartsWith("Pedido")));

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

            var opciones = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

            return lista.Select(h => new TablaApiHistorialDTO
            {
                IdHistorial = h.IdHistorial,
                IdRequisicion = h.IdRequisicion ?? 0,
                FechaGeneracion = h.FechaGeneracion,
                NombreUsuario = h.NombreUsuario,
                Observacion = h.Observacion,
                Modelo = System.Text.Json.JsonSerializer
                        .Deserialize<TablaApiEditableDTO>(h.DatosJson, opciones)
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
                p.Region = Z(p.Region);
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
            string sufAno = (anioActual % 100).ToString("D2");
            string prefijo = $"API-";
            string terminacion = $"/{sufAno}";

            int maxConsecutivo = 0;
            string ExtraerMaximo(string? valor)
            {
                if (valor == null) return null;
                var inicio = prefijo.Length;
                var fin = valor.Length - terminacion.Length;
                if (fin > inicio)
                {
                    var parte = valor.Substring(inicio, fin - inicio);
                    if (int.TryParse(parte, out int num) && num > maxConsecutivo)
                        maxConsecutivo = num;
                }
                return null;
            }

            var queryReq = await _repositoryRequisicion.Consultar(r =>
                r.NumApi != null &&
                r.NumApi.StartsWith(prefijo) &&
                r.NumApi.EndsWith(terminacion));
            var numApiReq = await queryReq.Select(r => r.NumApi).ToListAsync();
            foreach (var n in numApiReq) ExtraerMaximo(n);

            var queryCons = await _repoConsolidada.Consultar(c =>
                c.NumApi != null &&
                c.NumApi.StartsWith(prefijo) &&
                c.NumApi.EndsWith(terminacion));
            var numApiCons = await queryCons.Select(c => c.NumApi).ToListAsync();
            foreach (var n in numApiCons) ExtraerMaximo(n);

            return $"API-{(maxConsecutivo + 1):D4}/{sufAno}";
        }

        private async Task<string> GenerarNumeroPedidoAsync()
        {
            int anioActual = DateTime.Now.Year;
            string sufAno = (anioActual % 100).ToString("D2");
            string prefijo = "PED-";
            string terminacion = $"/{sufAno}";

            int maxConsecutivo = 0;
            string ExtraerMaximo(string? valor)
            {
                if (valor == null) return null;
                var inicio = prefijo.Length;
                var fin = valor.Length - terminacion.Length;
                if (fin > inicio)
                {
                    var parte = valor.Substring(inicio, fin - inicio);
                    if (int.TryParse(parte, out int num) && num > maxConsecutivo)
                        maxConsecutivo = num;
                }
                return null;
            }

            var queryReq = await _repositoryRequisicion.Consultar(r =>
                r.NumPedido != null &&
                r.NumPedido.StartsWith(prefijo) &&
                r.NumPedido.EndsWith(terminacion));
            var numPedReq = await queryReq.Select(r => r.NumPedido).ToListAsync();
            foreach (var n in numPedReq) ExtraerMaximo(n);

            var queryCons = await _repoConsolidada.Consultar(c =>
                c.NumPedido != null &&
                c.NumPedido.StartsWith(prefijo) &&
                c.NumPedido.EndsWith(terminacion));
            var numPedCons = await queryCons.Select(c => c.NumPedido).ToListAsync();
            foreach (var n in numPedCons) ExtraerMaximo(n);

            return $"PED-{(maxConsecutivo + 1):D4}/{sufAno}";
        }

        public async Task<string> AsegurarNumeroApiAsync(int idRequisicion)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null)
                throw new Exception($"No se encontró la requisición {idRequisicion}.");

            if (!string.IsNullOrEmpty(requisicion.NumApi))
                return requisicion.NumApi;

            var numApi = await GenerarNumeroApiAsync();
            requisicion.NumApi = numApi;
            await _repositoryRequisicion.Editar(requisicion);
            return numApi;
        }

        public async Task<string> AsegurarNumeroApiConsolidadaAsync(int idConsolidada)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null)
                throw new Exception($"No se encontró la consolidada {idConsolidada}.");

            if (!string.IsNullOrEmpty(consolidada.NumApi))
                return consolidada.NumApi;

            var numApi = await GenerarNumeroApiAsync();
            consolidada.NumApi = numApi;
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var hijas = await detallesQuery.Select(d => d.IdRequisicionNavigation).ToListAsync();

            foreach (var hija in hijas)
            {
                hija.NumApi = numApi;
                await _repositoryRequisicion.Editar(hija);
            }

            return numApi;
        }

        // DESPUÉS: jala el ganador guardado en BD
        private async Task<Dictionary<int, (decimal PrecioUnitario, decimal Cantidad, bool? IVA)>>
            ObtenerCotizacionConCantidadAsync(int idRequisicion)
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

            // ── Jalar el proveedor ganador desde BD ──
            var queryGanador = await _repositoryGanador.Consultar(
                g => g.IdRequisicion == idRequisicion);
            var ganador = await queryGanador.FirstOrDefaultAsync();
            var idProveedorGanador = ganador?.IdProveedor;

            // Si no hay ganador guardado, fallback al de menor total
            if (idProveedorGanador == null || idProveedorGanador <= 0)
            {
                var totalPorProveedor = cotizaciones
                    .GroupBy(c => c.IdProveedor)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(c =>
                        {
                            var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                            var precio = c.Importe!.Value * cant;
                            return c.Iva == true ? precio * 1.16m : precio;
                        }));

                idProveedorGanador = totalPorProveedor
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .FirstOrDefault();
            }

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
                    ClaveMaterial = d.IdArticuloNavigation != null ? (int?)d.IdArticuloNavigation.ClaveMaterial : null,
                    d.CogEditable,
                    d.NumPartida
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
                    c.Vigencia,
                    ProvNombre = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreProvedor : "",
                    ProvDireccion = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Direccion : "",
                    ProvRfc = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Rfc : ""
                })
                .ToListAsync();

            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad ?? 1m);

            var queryGanador = await _repositoryGanador.Consultar(g => g.IdRequisicion == idRequisicion);
            var ganador = await queryGanador.FirstOrDefaultAsync();
            int? idGanador = ganador?.IdProveedor;

            if (idGanador == null || idGanador <= 0)
            {
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

                idGanador = totalPorProveedor
                    .Where(kv => kv.Value > 0)
                    .OrderBy(kv => kv.Value)
                    .Select(kv => (int?)kv.Key)
                    .FirstOrDefault();
            }

            var provGanador = idGanador.HasValue
                ? cotizaciones.FirstOrDefault(c => c.IdProveedor == idGanador)
                : null;
            var vigenciaGanador = provGanador?.Vigencia;

            var cotGanadora = cotizaciones
                .Where(c => c.IdProveedor == idGanador && c.IdRequiDetalle.HasValue)
                .GroupBy(c => c.IdRequiDetalle!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            // ── Cargar importes autorizados del último historial TablaAPI ──────────
            var importesAutorizadosPorIndice = new Dictionary<int, decimal>();
            try
            {
                var queryHist = await _repoHistorial.Consultar(
                    h => h.IdRequisicion == idRequisicion
                      && (h.Observacion == null || h.Observacion != "Pedido"));
                var ultimaTablaApi = await queryHist
                    .OrderByDescending(h => h.FechaGeneracion)
                    .FirstOrDefaultAsync();

                if (ultimaTablaApi != null && !string.IsNullOrEmpty(ultimaTablaApi.DatosJson))
                {
                    var tablaApiDto = System.Text.Json.JsonSerializer
                        .Deserialize<TablaApiEditableDTO>(ultimaTablaApi.DatosJson);
                    if (tablaApiDto?.Partidas != null)
                    {
                        for (int idx = 0; idx < tablaApiDto.Partidas.Count; idx++)
                        {
                            var partida = tablaApiDto.Partidas[idx];
                            var importeStr = (partida.ImporteAutorizado ?? "")
                                .Replace("$", "").Replace(",", "").Trim();
                            if (decimal.TryParse(importeStr,
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out var totalConIva) && totalConIva > 0)
                            {
                                importesAutorizadosPorIndice[idx] = totalConIva;
                            }
                        }
                    }
                }
            }
            catch { /* si falla, usa precios del proveedor ganador sin modificar */ }

            var dto = new PedidoVistaDTO
            {
                IdRequisicion = idRequisicion,
                NumRequisicion = requisicion.NumRequisicion ?? "",
                NumeroPedido = requisicion.NumPedido ?? "",
                ProveedorNombre = provGanador?.ProvNombre ?? "",
                ProveedorDireccion = provGanador?.ProvDireccion ?? "",
                ProveedorRfc = provGanador?.ProvRfc ?? "",
                Departamento = nombreDepartamento,
                Responsable = requisicion.NomResponsableDepartamento ?? "",
                LugarEntrega = requisicion.LugarEntrega ?? "",
                PartidaPresupuestal = requisicion.IdPp?.ToString() ?? "",
                CondicionesPago = FormatearVigenciaPago(vigenciaGanador)
            };

            for (int i = 0; i < detalles.Count; i++)
            {
                var det = detalles[i];
                cotGanadora.TryGetValue(det.IdRequisicionDetalle, out var cot);
                bool tieneIva = cot?.Iva ?? false;
                decimal cantidad = det.Cantidad ?? 1m;
                decimal precioUnitario = cot?.Importe ?? 0m;

                if (importesAutorizadosPorIndice.TryGetValue(i, out var totalAutorizadoConIva))
                {
                    decimal totalSinIva = tieneIva
                        ? totalAutorizadoConIva / 1.16m
                        : totalAutorizadoConIva;
                    precioUnitario = cantidad > 0
                        ? Math.Round(totalSinIva / cantidad, 2)
                        : precioUnitario;
                }

                dto.Partidas.Add(new PedidoPartidaVistaDTO
                {
                    Numero = (i + 1).ToString(),
                    Clave = det.Clave ?? det.ClaveMaterial?.ToString() ?? "",
                    Descripcion = det.Descripcion ?? "",
                    Cantidad = cantidad,
                    UnidadMedida = det.UnidadMedida ?? "",
                    PrecioUnitario = precioUnitario,
                    TieneIva = tieneIva
                });
            }

            // ── Resolver EsEstatal por partida desde TblApiPartidas ───────────────
            var queryApiPartidas = await _repoApiPartidas.Consultar(
                p => p.IdRequisicion == idRequisicion);
            var apiPartidas = await queryApiPartidas
                .OrderByDescending(p => p.IdHistorial)
                .ToListAsync();

            bool hayPartidasApi = apiPartidas.Any();

            for (int i = 0; i < dto.Partidas.Count; i++)
            {
                var partida = dto.Partidas[i];
                bool? esEstatal = true; // conservador: si no hay datos, aplica retención

                if (hayPartidasApi)
                {
                    TblApiPartida? apiPartida = i < apiPartidas.Count ? apiPartidas[i] : null;

                    if (apiPartida == null)
                        apiPartida = apiPartidas.FirstOrDefault(p =>
                            string.Equals(p.ObjetoGasto, partida.Clave, StringComparison.OrdinalIgnoreCase));

                    if (apiPartida != null)
                        esEstatal = apiPartida.EsEstatal;
                }

                partida.EsEstatal = esEstatal;
            }

            // ── Totales con retención condicional ─────────────────────────────────
            int? idAdjudicacion = requisicion.IdAdjudicacion;

            decimal sumaEstatal = dto.Partidas
                .Where(p => p.EsEstatal == true)
                .Sum(p => p.PrecioUnitario * p.Cantidad);
            decimal sumaFederal = dto.Partidas
                .Where(p => p.EsEstatal == false)
                .Sum(p => p.PrecioUnitario * p.Cantidad);

            decimal sumaInicial = sumaEstatal + sumaFederal;
            decimal ivaInicial = sumaInicial * 0.16m;
            decimal subtotalInicial = sumaInicial + ivaInicial;

            bool aplicaRetencion = idAdjudicacion.HasValue && idAdjudicacion.Value > 1;
            decimal retencionInicial = aplicaRetencion ? sumaEstatal * 0.005m : 0m;

            dto.Suma = sumaInicial;
            dto.Iva = ivaInicial;
            dto.Descuento = 0m;
            dto.Subtotal = subtotalInicial;
            dto.Retencion = retencionInicial;
            dto.Total = subtotalInicial - retencionInicial;
            dto.SumaEstatal = sumaEstatal;
            dto.SumaFederal = sumaFederal;
            dto.AplicaRetencion = aplicaRetencion;

            return dto;
        }

        private static string FormatearVigenciaPago(int? vigencia)
        {
            if (!vigencia.HasValue || vigencia.Value <= 0)
                return "vigencia de cotizacion";

            return $"vigencia de {vigencia.Value} dias";
        }

        public async Task<byte[]> GenerarPedidoPdfAsync(PedidoVistaDTO form, string webRootPath)
        {
            var vista = form.IdConsolidada.HasValue
                ? await ObtenerPedidoEditableConsolidadaAsync(form.IdConsolidada.Value)
                : await ObtenerPedidoEditableAsync(form.IdRequisicion!.Value);
            vista.NumeroPedido = form.NumeroPedido;
            vista.TiempoEntrega = form.TiempoEntrega;
            vista.CondicionesPago = string.IsNullOrWhiteSpace(form.CondicionesPago)
                ? vista.CondicionesPago
                : form.CondicionesPago;

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
                    tblArt.AddCell(CVal(p.Numero, al: TextAlignment.CENTER));
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
            decimal sumaEstatal = form.SumaEstatal;
            decimal sumaFederal = form.SumaFederal;
            bool aplicaRetencion = form.AplicaRetencion;

            var tblTot = new Table(UnitValue.CreatePercentArray(new float[] { 70f, 18f, 12f }))
                .UseAllAvailableWidth();

            // Armar lista dinámica de filas
            var filas = new List<(string Label, string Valor, bool EsTotal, bool EsNota)>();

            // Desglose por fuente solo si hay mezcla
            if (sumaFederal > 0 && sumaEstatal > 0)
            {
                filas.Add(("  └ Recursos Federales", Fmt(sumaFederal), false, true));
                filas.Add(("  └ Recursos Estatales", Fmt(sumaEstatal), false, true));
            }

            filas.Add(("SUMA", Fmt(suma), false, false));
            filas.Add(("I.V.A. 16%", Fmt(iva), false, false));
            filas.Add(("DESCUENTO", Fmt(descuento), false, false));
            filas.Add(("SUBTOTAL", Fmt(subtotal), false, false));

            // Etiqueta de retención según aplique
            string labelRet = !aplicaRetencion
                ? "RET. 5 AL MILLAR\n(no aplica)"
                : (sumaFederal > 0
                    ? "RET. 5 AL MILLAR\n(solo rec. estatales)"
                    : "RET. 5 AL MILLAR\n(recursos estatales)");

            filas.Add((labelRet, Fmt(retencion), false, false));
            filas.Add(("TOTAL", Fmt(total), true, false));

            tblTot.AddCell(new Cell(filas.Count, 1)
                .SetBorder(borde).SetBackgroundColor(ColorConstants.WHITE));

            foreach (var (lbl, val, esTotal, esNota) in filas)
            {
                var bgLabel = esTotal ? PdfApiEstiloRequi.RosaAcento
                            : esNota ? new DeviceRgb(240, 244, 250)
                                       : PdfApiEstiloRequi.FondoEncabezadoTabla;
                var fgLabel = esTotal ? ColorConstants.WHITE
                                       : PdfApiEstiloRequi.TextoEncabezadoTabla;
                var bgVal = esTotal ? new DeviceRgb(255, 235, 240) : ColorConstants.WHITE;
                float szLabel = esNota ? 5.8f : 6.6f;

                tblTot.AddCell(new Cell().SetBorder(borde).SetBackgroundColor(bgLabel).SetPadding(4f)
                    .SetTextAlignment(TextAlignment.RIGHT).SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .Add(new Paragraph(lbl).SetFont(esNota ? regular : bold).SetFontSize(szLabel)
                        .SetFontColor(fgLabel)));
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
            var esServicio = (await _repositoryRequisicion.Obtener(r => r.IdRequisicion == form.IdRequisicion))?.RequiServicio ?? false;

            var firmantes = new (string Cargo, string Nombre)[]
            {
                esServicio
                    ? ("JEFE DE SECCIÓN DE SERVICIOS", "C. MOISÉS")
                    : ("JEFE DE SECCIÓN DE ADQUISICIONES", "C. ROGER ROJAS PÉREZ"),
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

            // ── SEGUNDA HOJA: CLÁUSULAS ───────────────────────────────────
            doc.Add(new AreaBreak(iText.Layout.Properties.AreaBreakType.NEXT_PAGE));

            var darkText = new DeviceRgb(30, 30, 30);
            var grayText = new DeviceRgb(80, 80, 80);
            var rosaH    = PdfApiEstiloRequi.RosaAcento;

            Paragraph TituloClausula(string txt) =>
                new Paragraph(txt)
                    .SetFont(bold).SetFontSize(7f).SetFontColor(rosaH)
                    .SetMarginTop(5f).SetMarginBottom(1f);

            Paragraph Clausula(string num, string texto) =>
                new Paragraph()
                    .Add(new Text(num + " ").SetFont(bold).SetFontColor(darkText))
                    .Add(new Text(texto).SetFont(regular).SetFontColor(darkText))
                    .SetFontSize(5.6f).SetMultipliedLeading(1.25f).SetMarginBottom(2f);

            // Encabezado de cláusulas
            var tblClauH = new Table(UnitValue.CreatePercentArray(new float[] { 100f }))
                .UseAllAvailableWidth();
            tblClauH.AddCell(new Cell().SetBorder(borde)
                .SetBackgroundColor(PdfApiEstiloRequi.FondoEncabezadoTabla).SetPadding(5f)
                .Add(new Paragraph("CLÁUSULAS PARA RECEPCIÓN Y TRÁMITE DE PEDIDOS")
                    .SetFont(bold).SetFontSize(9f).SetFontColor(rosaH)
                    .SetTextAlignment(TextAlignment.CENTER)));
            doc.Add(tblClauH);
            doc.Add(new Paragraph(" ").SetFontSize(2f));

            // Cuerpo: 2 columnas
            var tblBody = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f }))
                .UseAllAvailableWidth();

            // ── Columna izquierda ─────────────────────────────────────────
            var colIzq = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f).SetPaddingRight(5f);

            colIzq.Add(TituloClausula("1.  DEL PEDIDO"));
            colIzq.Add(Clausula("1.1.", "Este pedido se basa en la lista de precios aprobados, en poder del Sistema DIF Estatal o la cotización presentada por el proveedor."));
            colIzq.Add(Clausula("1.2.", "El proveedor acepta el presente pedido y se compromete a surtirlo en el plazo estipulado, por lo que cualquier aclaración sobre el contenido del mismo deberá efectuarse por escrito ante la Jefatura del Sistema DIF Estatal que haya tramitado la adquisición en un plazo máximo de tres días hábiles cuando se trate de artículos nacionales y diez días hábiles para artículos de importación directa y de importación adquiridos en el país, después de la fecha de recepción del pedido, transcurrido ese lapso ESTE SE CONSIDERA DEFINITIVAMENTE ACEPTADO."));
            colIzq.Add(Clausula("1.3.", "El Sistema DIF Estatal podrá cancelar este pedido total o parcialmente, si el proveedor no cumple con las condiciones establecidas en el mismo; en ambos casos, se hará efectiva la garantía de cumplimiento otorgada por el proveedor."));
            colIzq.Add(Clausula("1.4.", "Este pedido no es válido si presenta tachaduras, correcciones y/o alteraciones."));
            colIzq.Add(Clausula("1.5.", "Los gastos por conceptos de empaque, flete y acarreo, invariablemente correrán por cuenta del proveedor, con excepción de aquellos casos en que expresamente se establezcan en el pedido, que correrán a cargo del Sistema DIF Estatal."));
            colIzq.Add(Clausula("1.6.", "Todos los impuestos y derechos, tanto federales como estatales o municipales, de cualquier otra naturaleza, serán a cargo del proveedor, con excepción del Impuesto al Valor Agregado."));
            colIzq.Add(Clausula("1.7.", "El proveedor se obliga a dar las facilidades necesarias para que las dependencias del Sector Público Federal, Estatal y Municipal ejerzan las funciones que les concede la Ley de Adquisiciones, Arrendamientos y Servicios del Sector Público y su Reglamento, así como la Ley de Adquisiciones, Arrendamientos y Servicios del Sector Público Estatal y Municipal."));
            colIzq.Add(Clausula("1.8.", "El proveedor, para el cumplimiento de las obligaciones pactadas en el presente pedido, presentará garantía de cumplimiento, la cual, sólo podrá constituirse mediante fianza, cheque certificado o de caja o hipoteca por el 10% del monto total contratado, a favor del Gobierno del Estado de Puebla a través de la Secretaría de Planeación y Finanzas."));
            colIzq.Add(Clausula("1.9.", "Si el pedido es modificado por las partes, en términos de los artículos 112 Ley de Adquisiciones, Arrendamientos y Servicios del Sector Público Estatal y Municipal y/o 52 Ley de Adquisiciones, Arrendamientos y Servicios del Sector Público Federal, el proveedor se compromete a ampliar, disminuir o bien a modificar la garantía otorgada, según corresponda."));

            colIzq.Add(TituloClausula("2.  DE LA GARANTÍA DE CALIDAD Y DE LA INSPECCIÓN"));
            colIzq.Add(Clausula("2.1.", "El proveedor deberá garantizar la calidad de los productos ofrecidos y se obliga a su reposición, si al ser usados no corresponde a las especificaciones técnicas consignadas en el pedido."));
            colIzq.Add(Clausula("2.2.", "El Sistema DIF Estatal a través de Control de Calidad, efectuará pruebas sobre la calidad de los artículos, pudiendo rechazar aquellos que no reúnan las especificaciones requeridas."));
            colIzq.Add(Clausula("2.3.", "Independientemente de las pruebas que realice Control de Calidad, el proveedor responderá de los vicios ocultos que presenten los equipos, bienes y materiales entregados."));

            tblBody.AddCell(colIzq);

            // ── Columna derecha ───────────────────────────────────────────
            var colDer = new Cell().SetBorder(Border.NO_BORDER).SetPadding(0f).SetPaddingLeft(5f);

            colDer.Add(TituloClausula("3.  DE LA ENTREGA DE LOS ARTÍCULOS"));
            colDer.Add(Clausula("3.1.", "El proveedor acepta el presente pedido y se compromete a la entrega de cada uno de los bienes señalados, por la(s) cantidad(es) detallada(s), en el lugar y periodo indicado y con las especificaciones requeridas. El incumplimiento en los plazos de entrega o en las cantidades solicitadas será motivo de aplicación de una pena convencional consistente en la cantidad que corresponda a razón del 0.7% sobre el importe total de lo incumplido. Para efecto de la aplicación de esta sanción se tomará como fecha de entrega la indicada en el presente pedido."));
            colDer.Add(Clausula("3.2.", "La cancelación total o parcial de la(s) partida(s) adjudicada(s) en el presente pedido, será motivo de la aplicación de una pena convencional a razón del 10% sobre el importe total de la(s) partida(s) no entregada(s)."));
            colDer.Add(Clausula("3.3.", "Cuando el proveedor no pueda surtir los artículos solicitados en la fecha convenida por caso fortuito o fuerza mayor plenamente justificada, siempre y cuando no haya contribuido a ello, podrá solicitar por escrito una ampliación al plazo de entrega fijado; en la inteligencia que de concederse el plazo solicitado y este no surte el pedido, la sanción mencionada en el punto anterior se aplicará desde la fecha inicialmente estipulada."));
            colDer.Add(Clausula("3.4.", "Sólo podrán entregarse artículos distintos o que se consideren equivalentes a los estipulados en los pedidos, con autorización previa y por escrito del Sistema DIF Estatal."));
            colDer.Add(Clausula("3.5.", "Los bienes entregados que no cumplan con los requisitos y especificaciones solicitadas, serán sustituidos por el proveedor en un plazo de 48 horas, contadas a partir de que le sea notificado el bien que se encuentre con las deficiencias."));

            colDer.Add(TituloClausula("4.  DE LA FACTURACIÓN."));
            colDer.Add(Clausula("4.1.", "La(s) factura(s) deberá(n) describir los artículos y la misma redacción del pedido, mostrar claramente el número de pedido y el número de requisición."));
            colDer.Add(Clausula("4.2.", "La(s) factura(s) deberá(n) cumplir con los requisitos fiscales previstos en los artículos 29 y 29A del Código Fiscal de la Federación, acompañada(s) de los documento(s) que ampare el ingreso de los bienes en el almacén general del Sistema DIF Estatal, el cual contendrá sello y firma de recepción a entera satisfacción."));

            colDer.Add(TituloClausula("5.  DE LA FORMA DE PAGO."));
            colDer.Add(Clausula("5.1.", "El pago se realizará posterior a la entrega de los bienes recibidos a entera satisfacción, de acuerdo a lo siguiente:"));
            colDer.Add(new Paragraph("(   )  Tratándose de contratos fijos, dentro de los 30 días naturales siguientes a la presentación de la factura correspondiente.")
                .SetFont(regular).SetFontSize(5.6f).SetFontColor(darkText).SetMultipliedLeading(1.25f).SetMarginBottom(2f));
            colDer.Add(new Paragraph("(   )  Tratándose de contratos abiertos, dentro de los 30 días naturales siguientes a la presentación de la factura correspondiente a cada pedido que realice el Sistema DIF Estatal al proveedor.")
                .SetFont(regular).SetFontSize(5.6f).SetFontColor(darkText).SetMultipliedLeading(1.25f).SetMarginBottom(2f));
            colDer.Add(new Paragraph("(   )  A mes vencido, dentro de los 30 días naturales siguientes a la presentación de la factura correspondiente.")
                .SetFont(regular).SetFontSize(5.6f).SetFontColor(darkText).SetMultipliedLeading(1.25f).SetMarginBottom(2f));

            colDer.Add(TituloClausula("6.  DEL PAGO DE DERECHOS."));
            colDer.Add(Clausula("6.1.", "Con fundamento en el artículo 36 fracción V, de la Ley de Ingresos del Estado de Puebla, para el Ejercicio Fiscal 2025, este organismo de asistencia social procederá a retener al proveedor adjudicado el 5 al millar sobre el subtotal por cada factura generada."));

            tblBody.AddCell(colDer);
            doc.Add(tblBody);

            // ── Caja de obligación ────────────────────────────────────────
            doc.Add(new Paragraph(" ").SetFontSize(4f));
            var tblOblig = new Table(UnitValue.CreatePercentArray(new float[] { 100f })).UseAllAvailableWidth();
            tblOblig.AddCell(new Cell().SetBorder(borde).SetPadding(6f)
                .Add(new Paragraph("EL PROVEEDOR SE OBLIGA A ENTREGAR LOS BIENES EN LOS TÉRMINOS PACTADOS EN ESTE PEDIDO Y SUJETA A LAS DISPOSICIONES DE LA LEY DE ADQUISICIONES, ARRENDAMIENTOS Y SERVICIOS DEL SECTOR PÚBLICO ESTATAL Y MUNICIPAL Y/O LEY DE ADQUISICIONES, ARRENDAMIENTOS Y SERVICIOS DEL SECTOR PÚBLICO.")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(darkText).SetTextAlignment(TextAlignment.CENTER)));
            doc.Add(tblOblig);

            // ── Datos del representante ───────────────────────────────────
            doc.Add(new Paragraph(" ").SetFontSize(3f));
            var tblRep = new Table(UnitValue.CreatePercentArray(new float[] { 50f, 50f })).UseAllAvailableWidth();
            tblRep.AddCell(new Cell().SetBorder(borde).SetPadding(5f).SetMinHeight(18f)
                .Add(new Paragraph("NOMBRE DEL REPRESENTANTE").SetFont(bold).SetFontSize(6f).SetFontColor(grayText)));
            tblRep.AddCell(new Cell().SetBorder(borde).SetPadding(5f).SetMinHeight(18f)
                .Add(new Paragraph("CARGO").SetFont(bold).SetFontSize(6f).SetFontColor(grayText)));
            tblRep.AddCell(new Cell().SetBorder(borde).SetPadding(5f).SetMinHeight(18f)
                .Add(new Paragraph("TELÉFONO").SetFont(bold).SetFontSize(6f).SetFontColor(grayText)));
            tblRep.AddCell(new Cell().SetBorder(borde).SetPadding(5f).SetMinHeight(18f)
                .Add(new Paragraph("FECHA").SetFont(bold).SetFontSize(6f).SetFontColor(grayText)));
            doc.Add(tblRep);

            // ── Firma ─────────────────────────────────────────────────────
            doc.Add(new Paragraph(" ").SetFontSize(4f));
            var tblFirma = new Table(UnitValue.CreatePercentArray(new float[] { 100f })).UseAllAvailableWidth();
            tblFirma.AddCell(new Cell().SetBorder(borde).SetPadding(8f).SetMinHeight(55f)
                .SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.BOTTOM)
                .Add(new Paragraph("FIRMA").SetFont(bold).SetFontSize(6.5f).SetFontColor(grayText)));
            doc.Add(tblFirma);

            doc.Add(new Paragraph(" ").SetFontSize(4f));
            var tblAcredita = new Table(UnitValue.CreatePercentArray(new float[] { 100f })).UseAllAvailableWidth();
            tblAcredita.AddCell(new Cell().SetBorder(borde).SetPadding(6f)
                .Add(new Paragraph("EL REPRESENTANTE ACREDITA SU PODER PARA FIRMAR EL PEDIDO DE LA SIGUIENTE MANERA.")
                    .SetFont(bold).SetFontSize(6f).SetFontColor(darkText)));
            doc.Add(tblAcredita);

            doc.Close();
            return ms.ToArray();
        }

        public async Task GuardarHistorialPedidoAsync(
    PedidoVistaDTO modelo,
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
                Observacion = observacion ?? "Pedido"
            };

            await _repoHistorial.Crear(registro);
        }

        public async Task<List<PedidoHistorialDTO>> ObtenerHistorialPedidoAsync(int idRequisicion)
        {
            var query = await _repoHistorial.Consultar(
                h => h.IdRequisicion == idRequisicion
                  && h.Observacion != null
                  && h.Observacion.StartsWith("Pedido"));

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

            var opciones = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

            return lista.Select(h => new PedidoHistorialDTO
            {
                IdHistorial = h.IdHistorial,
                IdRequisicion = h.IdRequisicion ?? 0,
                FechaGeneracion = h.FechaGeneracion,
                NombreUsuario = h.NombreUsuario,
                Observacion = h.Observacion,
                Modelo = System.Text.Json.JsonSerializer
                .Deserialize<PedidoVistaDTO>(h.DatosJson, opciones)
            }).ToList();
        }

        public async Task<List<PedidoHistorialDTO>> ObtenerHistorialPedidoPorConsolidadaAsync(int idConsolidada)
        {
            var query = await _repoHistorial.Consultar(
                h => h.IdConsolidada == idConsolidada
                  && h.Observacion != null
                  && h.Observacion.StartsWith("Pedido"));

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

            var opciones = new System.Text.Json.JsonSerializerOptions
            {
                NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
                PropertyNameCaseInsensitive = true
            };

            return lista.Select(h => new PedidoHistorialDTO
            {
                IdHistorial = h.IdHistorial,
                IdRequisicion = h.IdRequisicion ?? 0,
                FechaGeneracion = h.FechaGeneracion,
                NombreUsuario = h.NombreUsuario,
                Observacion = h.Observacion,
                Modelo = System.Text.Json.JsonSerializer
                .Deserialize<PedidoVistaDTO>(h.DatosJson, opciones)
            }).ToList();
        }

        public async Task<PedidoVistaDTO> ObtenerPedidoEditableConsolidadaAsync(int idConsolidada)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada)
                ?? throw new Exception($"No se encontró la consolidada {idConsolidada}.");

            var detalleQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var detalles = await detalleQuery
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.TblRequisicionDetalles)
                        .ThenInclude(det => det.IdArticuloNavigation)
                .ToListAsync();

            var hijas = detalles.Select(d => d.IdRequisicionNavigation).ToList();
            var idsHijas = hijas.Select(r => (int?)r.IdRequisicion).ToList();

            var depto19 = await _repoDepartamento.Obtener(d => d.IdDepartamento == 19);
            var nombreDepartamento = depto19?.NombreDepartamento ?? "DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES";
            var responsableDepto = depto19?.NombreJefe ?? "";

            var todasPartidas = hijas
                .SelectMany(r => r.TblRequisicionDetalles.Where(a => a.Activo != false))
                .Select(a => new
                {
                    a.IdRequisicionDetalle,
                    a.IdRequisicion,
                    IdArticulo = a.IdArticulo ?? 0,
                    a.Descripcion,
                    a.Cantidad,
                    a.UnidadMedida,
                    Clave = a.IdArticuloNavigation != null ? a.IdArticuloNavigation.Clave : null,
                    ClaveMaterial = a.IdArticuloNavigation != null ? (int?)a.IdArticuloNavigation.ClaveMaterial : null,
                    a.NumPartida,
                    a.CogEditable
                })
                .ToList();

            var cotQuery = await _repositoryCotizaciones.Consultar(
                c => idsHijas.Contains(c.IdRequisicion) && c.IdRequiDetalle.HasValue && c.Importe.HasValue);
            var cotizaciones = await cotQuery
                .Select(c => new
                {
                    c.IdProveedor,
                    c.IdRequiDetalle,
                    c.Importe,
                    c.Iva,
                    c.Vigencia,
                    ProvNombre = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.NombreProvedor : "",
                    ProvDireccion = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Direccion : "",
                    ProvRfc = c.IdProveedorNavigation != null ? c.IdProveedorNavigation.Rfc : ""
                })
                .ToListAsync();

            var queryGanador = await _repositoryGanador.Consultar(g => idsHijas.Contains(g.IdRequisicion));
            var ganador = await queryGanador.FirstOrDefaultAsync();
            int? idGanador = ganador?.IdProveedor;

            if (idGanador == null || idGanador <= 0)
                throw new Exception($"La consolidada {idConsolidada} no tiene un proveedor ganador asignado.");

            var provGanador = idGanador.HasValue
                ? cotizaciones.FirstOrDefault(c => c.IdProveedor == idGanador)
                : null;
            var vigenciaGanador = provGanador?.Vigencia;

            var cotGanadora = cotizaciones
                .Where(c => c.IdProveedor == idGanador && c.IdRequiDetalle.HasValue)
                .GroupBy(c => c.IdRequiDetalle!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var partidasConsolidadas = todasPartidas
                .GroupBy(p => new { p.IdArticulo, p.Clave, p.ClaveMaterial, p.Descripcion, p.UnidadMedida })
                .Select(g =>
                {
                    var primera = g.First();
                    cotGanadora.TryGetValue(primera.IdRequisicionDetalle, out var cot);
                    bool tieneIva = cot?.Iva ?? false;
                    decimal precioUnitario = cot?.Importe ?? 0m;
                    decimal cantidadTotal = g.Sum(p => p.Cantidad ?? 1m);
                    return new
                    {
                        primera.IdRequisicionDetalle,
                        primera.Clave,
                        primera.ClaveMaterial,
                        primera.Descripcion,
                        primera.UnidadMedida,
                        primera.NumPartida,
                        Cantidad = cantidadTotal,
                        PrecioUnitario = precioUnitario,
                        TieneIva = tieneIva,
                        primera.CogEditable
                    };
                })
                .OrderBy(p => p.NumPartida)
                .ToList();

            var dto = new PedidoVistaDTO
            {
                IdRequisicion = null,
                IdConsolidada = idConsolidada,
                NumRequisicion = consolidada.FolioConsolidada ?? "",
                NumeroPedido = consolidada.NumPedido ?? "",
                ProveedorNombre = provGanador?.ProvNombre ?? "",
                ProveedorDireccion = provGanador?.ProvDireccion ?? "",
                ProveedorRfc = provGanador?.ProvRfc ?? "",
                Departamento = nombreDepartamento,
                Responsable = responsableDepto,
                LugarEntrega = "ALMACEN GENERAL",
                PartidaPresupuestal = consolidada.IdPp?.ToString() ?? "",
                CondicionesPago = FormatearVigenciaPago(vigenciaGanador)
            };

            foreach (var part in partidasConsolidadas)
            {
                dto.Partidas.Add(new PedidoPartidaVistaDTO
                {
                    Numero = (dto.Partidas.Count + 1).ToString(),
                    Clave = part.Clave ?? part.ClaveMaterial?.ToString() ?? "",
                    Descripcion = part.Descripcion ?? "",
                    Cantidad = part.Cantidad,
                    UnidadMedida = part.UnidadMedida ?? "",
                    PrecioUnitario = part.PrecioUnitario,
                    TieneIva = part.TieneIva
                });
            }

            // ── Resolver EsEstatal por partida desde TblApiPartidas ───────────────
            var queryApiPartidas = await _repoApiPartidas.Consultar(
                p => p.IdConsolidada == idConsolidada);
            var apiPartidas = await queryApiPartidas
                .OrderByDescending(p => p.IdHistorial)
                .ToListAsync();

            bool hayPartidasApi = apiPartidas.Any();

            for (int i = 0; i < dto.Partidas.Count; i++)
            {
                var partida = dto.Partidas[i];
                bool? esEstatal = true;

                if (hayPartidasApi)
                {
                    TblApiPartida? apiPartida = i < apiPartidas.Count ? apiPartidas[i] : null;

                    if (apiPartida == null)
                        apiPartida = apiPartidas.FirstOrDefault(p =>
                            string.Equals(p.ObjetoGasto, partida.Clave, StringComparison.OrdinalIgnoreCase));

                    if (apiPartida != null)
                        esEstatal = apiPartida.EsEstatal;
                }

                partida.EsEstatal = esEstatal;
            }

            // ── Totales con retención condicional ─────────────────────────────────
            int? idAdjudicacion = consolidada.IdAdjudicacion;

            decimal sumaEstatal = dto.Partidas
                .Where(p => p.EsEstatal == true)
                .Sum(p => p.PrecioUnitario * p.Cantidad);
            decimal sumaFederal = dto.Partidas
                .Where(p => p.EsEstatal == false)
                .Sum(p => p.PrecioUnitario * p.Cantidad);

            decimal sumaCons = sumaEstatal + sumaFederal;
            decimal ivaCons = sumaCons * 0.16m;
            decimal subtotalCons = sumaCons + ivaCons;

            bool aplicaRetencion = idAdjudicacion.HasValue && idAdjudicacion.Value > 1;
            decimal retencionCons = aplicaRetencion ? sumaEstatal * 0.005m : 0m;

            dto.Suma = sumaCons;
            dto.Iva = ivaCons;
            dto.Descuento = 0m;
            dto.Subtotal = subtotalCons;
            dto.Retencion = retencionCons;
            dto.Total = subtotalCons - retencionCons;
            dto.SumaEstatal = sumaEstatal;
            dto.SumaFederal = sumaFederal;
            dto.AplicaRetencion = aplicaRetencion;

            return dto;
        }

        public async Task GuardarHistorialPedidoConsolidadaAsync(
            PedidoVistaDTO modelo,
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
                IdConsolidada = modelo.IdConsolidada,
                IdRequisicion = null,
                IdUsuario = idUsuario,
                FechaGeneracion = DateTime.Now,
                DatosJson = json,
                Observacion = observacion ?? "Pedido"
            };

            await _repoHistorial.Crear(registro);
        }

        public async Task PoblarApiPartidasAsync(int idHistorial, TablaApiEditableDTO modelo)
        {
            // 1. Cargar todas las fuentes de financiamiento para resolver EsEstatal
            var queryFuentes = await _repoFuentes.Consultar();
            var fuentes = await queryFuentes.ToListAsync();
            var mapaFuentes = fuentes.ToDictionary(
                f => f.Clave ?? "",
                f => f,
                StringComparer.OrdinalIgnoreCase);

            // 2. Borrar partidas anteriores de este historial si existieran (idempotente)
            var queryExistentes = await _repoApiPartidas.Consultar(
                p => p.IdHistorial == idHistorial);
            var existentes = await queryExistentes.ToListAsync();
            foreach (var e in existentes)
                await _repoApiPartidas.Eliminar(e);

            // 3. Insertar una fila por cada partida del JSON
            foreach (var partida in modelo.Partidas ?? new())
            {
                var claveFuente = (partida.FuenteFinanciamiento ?? "").Trim();
                bool esEstatal = false;

                if (mapaFuentes.TryGetValue(claveFuente, out var fuente))
                    esEstatal = !string.Equals(fuente.Tipo, "Recursos federales",
                                    StringComparison.OrdinalIgnoreCase);

                // Parsear importes (vienen como "$25,520,000.00")
                decimal ParseImporte(string? s)
                {
                    if (string.IsNullOrWhiteSpace(s)) return 0m;
                    var limpio = s.Replace("$", "").Replace(",", "").Trim();
                    return decimal.TryParse(limpio,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var v) ? v : 0m;
                }

                await _repoApiPartidas.Crear(new TblApiPartida
                {
                    IdHistorial = idHistorial,
                    IdRequisicion = modelo.IdRequisicion > 0 ? modelo.IdRequisicion : null,
                    IdConsolidada = modelo.IdConsolidada > 0 ? modelo.IdConsolidada : null,
                    NumeroPartida = partida.Numero,
                    Ua = partida.Ua,
                    Region = partida.Region,
                    ClaveMunicipio = partida.ClaveMunicipio,
                    ClaveFuente = claveFuente,
                    Pp = partida.Pp,
                    Componente = partida.Componente,
                    Actividad = partida.Actividad,
                    ObjetoGasto = partida.ObjetoGasto,
                    ImporteSolicitado = ParseImporte(partida.ImporteSolicitado),
                    ImporteAutorizado = ParseImporte(partida.ImporteAutorizado),
                    EsEstatal = esEstatal
                });
            }
        }
    }
}
