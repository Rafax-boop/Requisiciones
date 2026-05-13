using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class ConsolidadaService : IConsolidadaService
    {
        private readonly IGenericRepository<TblRequisicion> _repoRequisicion;
        private readonly IGenericRepository<TblConsolidada> _repoConsolidada;
        private readonly IGenericRepository<TblConsolidadasDetalle> _repoDetalle;
        private readonly IGenericRepository<TblBitacoraEstatus> _repoBitacora;
        private readonly IGenericRepository<TblRegistroDiseno> _repoDiseno;

        public ConsolidadaService(
            IGenericRepository<TblRequisicion> repoRequisicion,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repoDetalle,
            IGenericRepository<TblBitacoraEstatus> repoBitacora,
            IGenericRepository<TblRegistroDiseno> repoDiseno)
        {
            _repoRequisicion = repoRequisicion;
            _repoConsolidada = repoConsolidada;
            _repoDetalle = repoDetalle;
            _repoBitacora = repoBitacora;
            _repoDiseno = repoDiseno;
        }

        public async Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConsolidables(int idUsuario)
        {
            var query = await _repoRequisicion.Consultar(r =>
                r.IdUsuarioMat == idUsuario &&
                r.IdEstatus == 2 &&
                r.ConsolidadaId == null);   // ← campo que agregaste con el ALTER

            return await query.Select(r => new RequisicionMaestraDTO
            {
                IdRequi = r.IdRequisicion,
                NumRequi = r.NumRequisicion,
                FechaEmision = r.FechaEmision,
                Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                Responsable = r.NomResponsableDepartamento,
                CantidadPartidas = r.TblRequisicionDetalles.Count
            }).ToListAsync();
        }

        public async Task<TblConsolidada> CrearConsolidada(List<int> idsRequisiciones, int idUsuario, bool servicio)
        {
            // Verificar que ninguna esté ya consolidada
            var query = await _repoRequisicion.Consultar(r =>
                idsRequisiciones.Contains(r.IdRequisicion));
            var requis = await query.ToListAsync();

            if (requis.Any(r => r.ConsolidadaId != null))
                return null;

            // Generar folio: CONS-YYYY-NNN
            var totalQuery = await _repoConsolidada.Consultar();
            var total = await totalQuery.CountAsync();
            var folio = $"CONS-{DateTime.Now.Year}-{(total + 1):D3}";
            var hash = BitConverter.ToString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)
            ).Replace("-", "");

            // Crear encabezado
            var consolidada = new TblConsolidada
            {
                FolioConsolidada = folio,
                Hash = hash,
                RequiServicio = servicio,
                IdUsuarioMat = idUsuario,
                IdUsuario = idUsuario,
                IdEstatus = 1,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
            var creada = await _repoConsolidada.Crear(consolidada);

            // Crear detalles (tabla puente)
            var detalles = idsRequisiciones.Select(id => new TblConsolidadasDetalle
            {
                ConsolidadaId = creada.ConsolidadaId,
                IdRequisicion = id,
                FechaAgregada = DateTime.Now,
                AgregadoPor = idUsuario
            }).ToList();
            await _repoDetalle.CrearRango(detalles);

            // Marcar cada requi como absorbida
            foreach (var r in requis)
            {
                r.ConsolidadaId = creada.ConsolidadaId;
                await _repoRequisicion.Editar(r);
            }

            return creada;
        }

        public async Task<ConsolidadaDetalleDTO> ObtenerDetalleConsolidada(int idConsolidada)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null) return null;

            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var detalles = await detallesQuery
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.TblRequisicionDetalles)
                .ToListAsync();

            var requisHijas = detalles.Select(d => new RequiHijaDTO
            {
                IdRequi = d.IdRequisicion,
                NumRequi = d.IdRequisicionNavigation.NumRequisicion,
                Departamento = d.IdRequisicionNavigation.IdDepartamentoNavigation.NombreDepartamento,
                Responsable = d.IdRequisicionNavigation.NomResponsableDepartamento,
                CantidadPartidas = d.IdRequisicionNavigation.TblRequisicionDetalles
                                    .Count(x => x.Activo != false)
            }).ToList();

            var articulos = detalles
                .SelectMany(d => d.IdRequisicionNavigation.TblRequisicionDetalles
                    .Where(a => a.Activo != false)
                    .Select(a => new ArticuloConsolidadoDTO
                    {
                        NumRequi = d.IdRequisicionNavigation.NumRequisicion,
                        NumPartida = a.NumPartida,
                        Cantidad = a.Cantidad,
                        UnidadMedida = a.UnidadMedida,
                        Descripcion = a.Descripcion,
                        DescripcionDetallada = a.DescripcionDetallada
                    }))
                .OrderBy(a => a.NumRequi)
                .ThenBy(a => a.NumPartida)
                .ToList();

            // Datos del encabezado — navegaciones simples
            var estatusNombre = consolidada.IdEstatusNavigation?.NombreEstatus ?? "";
            var creadoPor = consolidada.IdUsuarioNavigation?.Usuario ?? "";

            // Si las navegaciones no están cargadas, hacer consulta separada
            if (string.IsNullOrEmpty(estatusNombre))
            {
                var conQuery = await _repoConsolidada.Consultar(c => c.ConsolidadaId == idConsolidada);
                var con = await conQuery
                    .Include(c => c.IdEstatusNavigation)
                    .Include(c => c.IdUsuarioNavigation)
                    .FirstOrDefaultAsync();
                estatusNombre = con?.IdEstatusNavigation?.NombreEstatus ?? "";
                creadoPor = con?.IdUsuarioNavigation?.Usuario ?? "";
            }

            var queryDocs = await _repoDiseno.Consultar(f => f.IdConsolidada == idConsolidada);

            var docsCuadro = await queryDocs
                .Where(f => f.Tipo == "cuadro_comparativo")
                .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                .ToListAsync();

            var docsAnexos = await queryDocs
                .Where(f => f.Tipo == "anexo")
                .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                .ToListAsync();

            return new ConsolidadaDetalleDTO
            {
                ConsolidadaId = consolidada.ConsolidadaId,
                FolioConsolidada = consolidada.FolioConsolidada,
                Estatus = estatusNombre,
                FechaCreacion = consolidada.FechaCreacion.ToString("dd/MM/yyyy"),
                CreadoPor = creadoPor,
                Requisiciones = requisHijas,
                Articulos = articulos,
                CuadroComparativo = docsCuadro,
                Anexos = docsAnexos
            };
        }

        public async Task<List<ConsolidadaDTO>> ListarConsolidadas(bool servicio, int? idUsuario = null)
        {
            IQueryable<TblConsolidada> query;

            if (idUsuario.HasValue)
            {
                // Analista: solo las asignadas a él como responsable de materiales
                var raw = await _repoConsolidada.Consultar(c => c.IdUsuarioMat == idUsuario.Value && c.RequiServicio == servicio);
                query = raw;
            }
            else
            {
                // Admin: todas
                var raw = await _repoConsolidada.Consultar(c => c.RequiServicio == servicio);
                query = raw;
            }

            return await query
                .Select(c => new ConsolidadaDTO
                {
                    ConsolidadaID = c.ConsolidadaId,
                    FolioConsolidada = c.FolioConsolidada,
                    FechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy"),
                    IdEstatus = c.IdEstatus,
                    Estatus = c.IdEstatusNavigation.NombreEstatus,
                    CreadoPor = c.IdUsuarioNavigation.Usuario,
                    CantidadRequis = c.TblConsolidadasDetalles.Count,
                    TotalPartidas = c.TblConsolidadasDetalles
                                         .SelectMany(d => d.IdRequisicionNavigation.TblRequisicionDetalles)
                                         .Count(),
                    Departamentos = string.Join(", ", c.TblConsolidadasDetalles
                                         .Select(d => d.IdRequisicionNavigation
                                                       .IdDepartamentoNavigation.NombreDepartamento)
                                         .Distinct())
                })
                .OrderByDescending(c => c.ConsolidadaID)
                .ToListAsync();
        }

        public async Task<bool> AtenderConsolidada(AtenderConsolidadaDTO modelo, int idUsuario)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == modelo.IdConsolidada);
            if (consolidada == null) return false;

            // Guardar datos en la consolidada
            consolidada.IdPp = modelo.IdPP;
            consolidada.Ff = modelo.FF;
            consolidada.TipoPrograma = modelo.TipoPrograma;
            consolidada.IdEstatus = 13;  // mismo estatus que requi individual al atender
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            // Propagar a todas las hijas
            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == modelo.IdConsolidada);
            var detalles = await detallesQuery.ToListAsync();
            var idsHijas = detalles.Select(d => d.IdRequisicion).ToList();

            var requisQuery = await _repoRequisicion.Consultar(r => idsHijas.Contains(r.IdRequisicion));
            var requis = await requisQuery.ToListAsync();

            foreach (var r in requis)
            {
                r.IdPp = modelo.IdPP;
                r.Ff = modelo.FF;
                r.TipoPrograma = modelo.TipoPrograma;
                r.IdEstatus = 13;
                r.FechaModificacion = DateTime.Now;
                await _repoRequisicion.Editar(r);

                await _repoBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = r.IdRequisicion,
                    IdEstatus = 13,
                    FechaEstatus = DateTime.Now,
                    Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] {modelo.Observaciones}",
                    IdUsuario = idUsuario
                });
            }

            return true;
        }

        public async Task<bool> GuardarArchivosAtencionConsolidada(
            int idConsolidada,
            List<IFormFile> cuadroComparativo,
            List<IFormFile> anexos,
            string webRootPath)
        {
            var carpeta = $"consolidada_{idConsolidada}";

            async Task Guardar(List<IFormFile> archivos, string carpetaNombre, string tipo)
            {
                if (!archivos.Any()) return;
                var rutaBase = Path.Combine(webRootPath, "uploads", carpetaNombre, carpeta);
                Directory.CreateDirectory(rutaBase);

                foreach (var archivo in archivos)
                {
                    if (archivo.Length == 0) continue;
                    var nombre = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                    using (var stream = new FileStream(Path.Combine(rutaBase, nombre), FileMode.Create))
                        await archivo.CopyToAsync(stream);

                    await _repoDiseno.Crear(new TblRegistroDiseno
                    {
                        IdConsolidada = idConsolidada,
                        Ruta = $"/uploads/{carpetaNombre}/{carpeta}/{nombre}",
                        FechaSubida = DateTime.Now,
                        Tipo = tipo
                    });
                }
            }

            await Guardar(cuadroComparativo, "cuadro_comparativo", "cuadro_comparativo");
            await Guardar(anexos, "Anexos", "anexo");
            return true;
        }

        public async Task<TblConsolidada?> ObtenerConsolidada(int idConsolidada)
        {
            return await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
        }

        public async Task<List<PartidaConsolidadaDTO>> ObtenerPartidasConsolidada(int idConsolidada)
        {
            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var detalles = await detallesQuery
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.TblRequisicionDetalles)
                .ToListAsync();

            var todasLasPartidas = detalles
                .SelectMany(d => d.IdRequisicionNavigation.TblRequisicionDetalles
                    .Where(p => p.Activo != false))
                .ToList();

            // Agrupar por IdArticulo
            var grupos = todasLasPartidas
                .GroupBy(p => p.IdArticulo)
                .OrderBy(g => g.Key)
                .Select((g, idx) => new PartidaConsolidadaDTO
                {
                    IdRequiDetalle = g.First().IdRequisicionDetalle, // representante del grupo
                    IdArticulo = g.Key,
                    Descripcion = g.First().Descripcion,
                    DescripcionDetallada = g.First().DescripcionDetallada,
                    CantidadTotal = g.Sum(p => p.Cantidad ?? 0),
                    UnidadMedida = g.First().UnidadMedida,
                    NumPartida = idx + 1,
                    IdsRequiDetalle = g.Select(p => p.IdRequisicionDetalle).ToList(),
                    IdRequisicionPorDetalle = g.ToDictionary(
                        p => p.IdRequisicionDetalle,
                        p => p.IdRequisicion
                    )
                })
                .ToList();

            return grupos;
        }
    }
}
