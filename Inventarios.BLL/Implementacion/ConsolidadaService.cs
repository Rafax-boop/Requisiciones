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
        private readonly IGenericRepository<TblEstatus> _repoEstatus;
        private readonly IGenericRepository<TblRegistroDiseno> _repoDiseno;
        private readonly IGenericRepository<TblFormato> _repoFormato;
        private readonly IGenericRepository<TblRequisicionDetalleMovimiento> _repoMovimiento;
        private readonly IGenericRepository<TblPedido> _repoPedido;

        public ConsolidadaService(
            IGenericRepository<TblRequisicion> repoRequisicion,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repoDetalle,
            IGenericRepository<TblBitacoraEstatus> repoBitacora,
            IGenericRepository<TblEstatus> repoEstatus,
            IGenericRepository<TblRegistroDiseno> repoDiseno,
            IGenericRepository<TblFormato> repoFormato,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento,
            IGenericRepository<TblPedido> repoPedido)
        {
            _repoRequisicion = repoRequisicion;
            _repoConsolidada = repoConsolidada;
            _repoDetalle = repoDetalle;
            _repoBitacora = repoBitacora;
            _repoEstatus = repoEstatus;
            _repoDiseno = repoDiseno;
            _repoFormato = repoFormato;
            _repoMovimiento = repoMovimiento;
            _repoPedido = repoPedido;
        }

        public async Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConsolidables()
        {
            var query = await _repoRequisicion.Consultar(r =>
                r.IdEstatus == 1 &&
                r.ConsolidadaId == null);

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

            // Generar folio: CONS-TIPO-YYYY-NNN
            var totalQuery = await _repoConsolidada.Consultar();
            var total = await totalQuery.CountAsync();
            var tipoFolio = servicio ? "SER" : "ADQ";
            var folio = $"CONS-{tipoFolio}-{DateTime.Now.Year}-{(total + 1):D3}";
            var hash = BitConverter.ToString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)
            ).Replace("-", "");

            // Crear encabezado
            var consolidada = new TblConsolidada
            {
                FolioConsolidada = folio,
                Hash = hash,
                RequiServicio = servicio,
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
            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var detalles = await detallesQuery
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(d => d.IdRequisicionNavigation)
                    .ThenInclude(r => r.TblRequisicionDetalles)
                .ToListAsync();

            var consolidada = await (await _repoConsolidada.Consultar(c => c.ConsolidadaId == idConsolidada))
                .Include(c => c.IdEstatusNavigation)
                .Include(c => c.IdUsuarioNavigation)
                .FirstOrDefaultAsync();

            if (consolidada == null) return null;

            var requisHijas = detalles
                .Where(d => d.IdRequisicionNavigation != null)
                .Select(d => new RequiHijaDTO
            {
                IdRequi = d.IdRequisicion,
                NumRequi = d.IdRequisicionNavigation!.NumRequisicion ?? "Sin folio",
                Departamento = d.IdRequisicionNavigation.IdDepartamentoNavigation?.NombreDepartamento ?? "Sin departamento",
                Responsable = d.IdRequisicionNavigation.NomResponsableDepartamento ?? "Sin responsable",
                CantidadPartidas = d.IdRequisicionNavigation.TblRequisicionDetalles?
                                    .Count(x => x.Activo != false) ?? 0
            }).ToList();

            var articulos = detalles
                .Where(d => d.IdRequisicionNavigation != null)
                .SelectMany(d => (d.IdRequisicionNavigation!.TblRequisicionDetalles ?? new List<TblRequisicionDetalle>())
                    .Where(a => a.Activo != false)
                    .Select(a => new ArticuloConsolidadoDTO
                    {
                        NumRequi = d.IdRequisicionNavigation!.NumRequisicion ?? "Sin folio",
                        NumPartida = a.NumPartida,
                        Cantidad = a.Cantidad,
                        UnidadMedida = a.UnidadMedida ?? "",
                        Descripcion = a.Descripcion ?? "",
                        DescripcionDetallada = a.DescripcionDetallada ?? ""
                    }))
                .OrderBy(a => a.NumRequi)
                .ThenBy(a => a.NumPartida)
                .ToList();

            var estatusNombre = consolidada.IdEstatusNavigation?.NombreEstatus ?? "";
            var creadoPor = consolidada.IdUsuarioNavigation?.Usuario ?? "";

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
                    var nombre = $"{Guid.NewGuid().ToString("N").Substring(0, 8)}_{Path.GetFileName(archivo.FileName)}";
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

        public async Task<List<ProgresoPasoDTO>> ObtenerProgresoConsolidada(int idConsolidada)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null)
                return new List<ProgresoPasoDTO>();

            var nombresEstatus = await _repoEstatus.Consultar();
            var dictEstatus = await nombresEstatus.ToDictionaryAsync(e => e.IdEstatus, e => e.NombreEstatus);

            var esMx = System.Globalization.CultureInfo.GetCultureInfo("es-MX");
            var resultado = new List<ProgresoPasoDTO>();

            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var idsHijas = await detallesQuery.Select(d => d.IdRequisicion).ToListAsync();

            if (idsHijas.Count == 0)
            {
                resultado.Add(CrearPasoSintetico(consolidada, dictEstatus, esMx, "active"));
                return resultado;
            }

            var bitacoraQuery = await _repoBitacora.Consultar(b => b.IdRequisicion.HasValue && idsHijas.Contains(b.IdRequisicion.Value));
            var eventosDb = await bitacoraQuery
                .Include(b => b.IdUsuarioNavigation)
                .OrderBy(b => b.FechaEstatus)
                .ThenBy(b => b.IdBitacoraEstatus)
                .ToListAsync();

            var requisQuery = await _repoRequisicion.Consultar(r => idsHijas.Contains(r.IdRequisicion));
            var dictFolios = await requisQuery
                .ToDictionaryAsync(r => r.IdRequisicion, r => r.NumRequisicion ?? "Sin folio");

            for (int i = 0; i < eventosDb.Count; i++)
            {
                var ev = eventosDb[i];
                int idEst = ev.IdEstatus ?? 0;
                bool esUltimo = i == eventosDb.Count - 1;

                string state = "active";
                if (!esUltimo)
                    state = "done";
                else if (EstatusFlow.EsTerminalNegativo(idEst))
                    state = "cancelled";
                else if (EstatusFlow.TerminalPositivos.Contains(idEst))
                    state = "completed";

                string date = "—", time = "—";
                if (ev.FechaEstatus.HasValue)
                {
                    var dt = ev.FechaEstatus.Value;
                    date = dt.ToString("dd MMM yyyy", esMx);
                    time = dt.ToString("hh:mm tt", esMx);
                }

                var folio = ev.IdRequisicion.HasValue && dictFolios.TryGetValue(ev.IdRequisicion.Value, out var f) ? f : "Sin folio";
                var obs = string.IsNullOrWhiteSpace(ev.Observacion)
                    ? $"[{folio}]"
                    : $"[{folio}] {ev.Observacion}";

                resultado.Add(new ProgresoPasoDTO
                {
                    Dept = dictEstatus.GetValueOrDefault(idEst, $"Estatus {idEst}"),
                    Date = date,
                    State = state,
                    By = string.IsNullOrWhiteSpace(ev.IdUsuarioNavigation?.Usuario) ? "—" : ev.IdUsuarioNavigation.Usuario,
                    Time = time,
                    Action = obs,
                    Comment = obs
                });
            }

            if (consolidada.IdEstatus > 0 && (eventosDb.Count == 0 || (eventosDb.Last().IdEstatus ?? 0) != consolidada.IdEstatus))
            {
                string synState = EstatusFlow.EsTerminalNegativo(consolidada.IdEstatus) ? "cancelled"
                    : EstatusFlow.TerminalPositivos.Contains(consolidada.IdEstatus) ? "completed"
                    : "active";

                resultado.Add(CrearPasoSintetico(consolidada, dictEstatus, esMx, synState));
            }

            return resultado;
        }

        private static ProgresoPasoDTO CrearPasoSintetico(TblConsolidada consolidada, Dictionary<int, string> dictEstatus, System.Globalization.CultureInfo esMx, string state)
        {
            var fechaBase = consolidada.FechaModificacion ?? consolidada.FechaCreacion;
            return new ProgresoPasoDTO
            {
                Dept = dictEstatus.GetValueOrDefault(consolidada.IdEstatus, $"Estatus {consolidada.IdEstatus}"),
                Date = fechaBase.ToString("dd MMM yyyy", esMx),
                State = state,
                By = "—",
                Time = fechaBase.ToString("hh:mm tt", esMx),
                Action = "",
                Comment = ""
            };
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

        public async Task<List<ConsolidadaVerificadaDTO>> ObtenerConsolidadasVerificadas(int idUsuario, bool servicio)
        {
            IQueryable<TblConsolidada> consBase;

            if (!servicio)
            {
                consBase = await _repoConsolidada.Consultar(c =>
                    c.IdUsuarioMat == idUsuario &&
                    c.RequiServicio == false);
            }
            else
            {
                consBase = await _repoConsolidada.Consultar(c =>
                    c.RequiServicio == true);
            }

            var consolidadas = await consBase
                .Include(c => c.IdEstatusNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                    .ThenInclude(d => d.IdRequisicionNavigation)
                        .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                    .ThenInclude(d => d.IdRequisicionNavigation)
                        .ThenInclude(r => r.TblRequisicionDetalles)
                .ToListAsync();

            var estatusValidos = servicio ? new[] { 15, 18 } : new[] { 15, 18 };

            var resultado = consolidadas
                .Where(c => c.TblConsolidadasDetalles
                    .Any(d => estatusValidos.Contains(d.IdRequisicionNavigation.IdEstatus ?? 0)))
                .Select(c =>
                {
                    var hijas = c.TblConsolidadasDetalles
                        .Select(d => d.IdRequisicionNavigation)
                        .ToList();

                    return new ConsolidadaVerificadaDTO
                    {
                        ConsolidadaId = c.ConsolidadaId,
                        FolioConsolidada = c.FolioConsolidada,
                        FechaCreacion = c.FechaCreacion.ToString("dd/MM/yyyy"),
                        IdEstatus = c.IdEstatus,
                        Estatus = c.IdEstatusNavigation?.NombreEstatus ?? "",
                        CantidadRequisiciones = hijas.Count,
                        TotalPartidas = hijas.Sum(r => r.TblRequisicionDetalles.Count(d => d.Activo != false)),
                        Departamentos = string.Join(", ", hijas
                            .Select(r => r.IdDepartamentoNavigation?.NombreDepartamento ?? "")
                            .Distinct()),
                        RequiServicio = c.RequiServicio ?? false
                    };
                }).ToList();

            return resultado;
        }

        public async Task<ConsolidadaExpedienteDTO> ObtenerExpedienteConsolidada(int idConsolidada)
        {
            var consQuery = await _repoConsolidada.Consultar(c => c.ConsolidadaId == idConsolidada);
            var consolidada = await consQuery
                .Include(c => c.IdEstatusNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                    .ThenInclude(d => d.IdRequisicionNavigation)
                        .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                    .ThenInclude(d => d.IdRequisicionNavigation)
                        .ThenInclude(r => r.TblRequisicionDetalles)
                            .ThenInclude(a => a.IdArticuloNavigation)
                .Include(c => c.TblConsolidadasDetalles)
                    .ThenInclude(d => d.IdRequisicionNavigation)
                        .ThenInclude(r => r.TblBitacoraEstatuses)
                .FirstOrDefaultAsync();

            if (consolidada == null) return null;

            var hijas = consolidada.TblConsolidadasDetalles
                .Select(d => d.IdRequisicionNavigation)
                .ToList();

            var requisHijas = hijas.Select(r => new RequiHijaDTO
            {
                IdRequi = r.IdRequisicion,
                NumRequi = r.NumRequisicion,
                Departamento = r.IdDepartamentoNavigation?.NombreDepartamento ?? "",
                Responsable = r.NomResponsableDepartamento,
                CantidadPartidas = r.TblRequisicionDetalles.Count(d => d.Activo != false)
            }).ToList();

            var articulos = hijas
                .SelectMany(r => r.TblRequisicionDetalles
                    .Where(a => a.Activo != false)
                    .Select(a => new DetalleArticuloDTO
                    {
                        IdRequisicionDetalle = a.IdRequisicionDetalle,
                        NumPartida = a.NumPartida,
                        ClaveMaterial = a.IdArticuloNavigation?.Clave,
                        IdArticulo = a.IdArticulo,
                        Cantidad = a.Cantidad,
                        UnidadMedida = a.UnidadMedida,
                        Descripcion = a.Descripcion,
                        DescripcionDetallada = a.DescripcionDetallada,
                        NumRequiOrigen = r.NumRequisicion
                    }))
                .OrderBy(a => a.NumPartida)
                .ToList();

            // Poblar CantidadAlmacen desde movimientos COMPRA
            var idsDetalles = articulos.Select(a => a.IdRequisicionDetalle).ToList();
            var queryMov = await _repoMovimiento.Consultar(m => idsDetalles.Contains(m.IdRequisicionDetalle)
                && m.TipoMovimiento == "COMPRA"
                && m.CantidadMovimiento != m.CantidadOriginal);
            var movimientos = await queryMov.ToListAsync();
            foreach (var art in articulos)
            {
                var mov = movimientos.FirstOrDefault(m => m.IdRequisicionDetalle == art.IdRequisicionDetalle);
                if (mov != null)
                    art.CantidadAlmacen = mov.CantidadMovimiento;
            }

            var observaciones = hijas
                .SelectMany(r => r.TblBitacoraEstatuses
                    .Where(b => b.IdEstatus == 13 || b.IdEstatus == 15 || b.IdEstatus == 16 || b.IdEstatus == 18))
                .OrderByDescending(b => b.FechaEstatus)
                .Select(b => b.Observacion)
                .FirstOrDefault();

            var numApi = hijas
                .Select(r => r.NumApi)
                .FirstOrDefault(n => !string.IsNullOrEmpty(n));

            var docsQuery = await _repoDiseno.Consultar(f => f.IdConsolidada == idConsolidada);
            var docs = await docsQuery.ToListAsync();

            var queryPedidos = await _repoPedido.Consultar(p => p.IdConsolidada == idConsolidada);
            var numPedidos = await queryPedidos.Select(p => p.TipoRecurso).Distinct().ToListAsync();

            return new ConsolidadaExpedienteDTO
            {
                ConsolidadaId = consolidada.ConsolidadaId,
                FolioConsolidada = consolidada.FolioConsolidada,
                IdEstatus = consolidada.IdEstatus,
                Estatus = consolidada.IdEstatusNavigation?.NombreEstatus ?? "",
                FechaCreacion = consolidada.FechaCreacion.ToString("dd/MM/yyyy"),
                IdPp = consolidada.IdPp,
                Ff = consolidada.Ff,
                TipoPrograma = consolidada.TipoPrograma,
                NumeroApi = numApi,
                Observaciones = observaciones,
                Requisiciones = requisHijas,
                Articulos = articulos,
                CuadroComparativo = docs
                    .Where(f => f.Tipo == "cuadro_comparativo")
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                    .ToList(),
                Anexos = docs
                    .Where(f => f.Tipo == "anexo")
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                    .ToList(),
                ArchivosSiaf = docs
                    .Where(f => f.Tipo == "SIAF")
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                    .ToList(),
                ArchivosTablaApi = docs
                    .Where(f => f.Tipo == "TablaApi")
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                    .ToList(),
                ArchivosPedidoCompra = docs
                    .Where(f => f.Tipo.StartsWith("pedido_compra"))
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = Path.GetFileName(f.Ruta) })
                    .ToList(),
                DocumentosProveedor = docs
                    .Where(f => f.Tipo.StartsWith("proveedor_"))
                    .Select(f => new ArchivoAtencionDTO { Ruta = f.Ruta, NombreArchivo = f.Tipo.Replace("proveedor_", "") })
                    .ToList(),
                NumPedidos = numPedidos
            };
        }

        public async Task<bool> SubirDocumentoProveedorConsolidada(int idConsolidada, string tipoDocumento,
            IFormFile archivo, string webRootPath, int idUsuario)
        {
            if (archivo == null || archivo.Length == 0) return false;

            var carpeta = $"consolidada_{idConsolidada}";
            var rutaBase = Path.Combine(webRootPath, "uploads", "Proveedor", carpeta);
            Directory.CreateDirectory(rutaBase);

            var nombre = $"{Guid.NewGuid().ToString("N").Substring(0, 8)}_{Path.GetFileName(archivo.FileName)}";
            using (var stream = new FileStream(Path.Combine(rutaBase, nombre), FileMode.Create))
                await archivo.CopyToAsync(stream);

            var filtroPrev = await _repoDiseno.Consultar(f =>
                f.IdConsolidada == idConsolidada && f.Tipo == $"proveedor_{tipoDocumento}");
            var previos = await filtroPrev.ToListAsync();
            foreach (var p in previos)
                await _repoDiseno.Eliminar(p);

            await _repoDiseno.Crear(new TblRegistroDiseno
            {
                IdConsolidada = idConsolidada,
                Ruta = $"/uploads/Proveedor/{carpeta}/{nombre}",
                FechaSubida = DateTime.Now,
                Tipo = $"proveedor_{tipoDocumento}"
            });

            return true;
        }

        public async Task<List<ArchivoAtencionDTO>> ObtenerDocumentosProveedorConsolidada(int idConsolidada)
        {
            var query = await _repoDiseno.Consultar(f =>
                f.IdConsolidada == idConsolidada && f.Tipo.StartsWith("proveedor_"));

            var docs = await query.Select(f => new ArchivoAtencionDTO
            {
                Ruta = f.Ruta,
                NombreArchivo = f.Tipo.Replace("proveedor_", "")
            }).ToListAsync();

            // Formato de Entrega - desde formatos de entrada firmados de almacén (solo materiales)
            var hijosQuery = await _repoRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var hijos = await hijosQuery.ToListAsync();
            var idsRequisicion = hijos.Select(r => r.IdRequisicion).ToList();

            var esServicio = hijos.Any(r => r.RequiServicio == true);

            if (idsRequisicion.Any() && !esServicio)
            {
                var formatosQuery = await _repoFormato.Consultar(f =>
                    idsRequisicion.Contains(f.IdRequisicion ?? 0) && f.TipoFormato == "ENTRADA" && f.RutaArchivo != "PENDIENTE");
                var formatos = await formatosQuery.ToListAsync();

                foreach (var fmt in formatos)
                {
                    docs.Add(new ArchivoAtencionDTO
                    {
                        Ruta = fmt.RutaArchivo,
                        NombreArchivo = "FormatoEntrega",
                        Label = $"Formato de Entrada #{fmt.NumeroFormato} ({fmt.FechaFormato:dd/MM/yyyy})"
                    });
                }
            }

            return docs;
        }

        public async Task<List<ArchivoRequisicionDTO>> ObtenerArchivosConsolidada(int idConsolidada)
        {
            var queryDisenos = await _repoDiseno.Consultar(f => f.IdConsolidada == idConsolidada);
            var disenos = await queryDisenos.OrderByDescending(a => a.FechaSubida).ToListAsync();

            var queryFormatos = await _repoFormato.Consultar(f =>
                f.IdConsolidada == idConsolidada
                && (f.TipoFormato == "ENTRADA" || f.TipoFormato == "SALIDA")
                && f.RutaArchivo != "PENDIENTE");
            var formatos = await queryFormatos.ToListAsync();

            var resultado = new List<ArchivoRequisicionDTO>();
            resultado.AddRange(disenos.Select(d => new ArchivoRequisicionDTO
            {
                Id = d.Id,
                Tipo = d.Tipo,
                Ruta = d.Ruta,
                FechaSubida = d.FechaSubida
            }));
            resultado.AddRange(formatos.Select(f => new ArchivoRequisicionDTO
            {
                Id = f.IdFormato,
                Tipo = f.TipoFormato,
                Ruta = f.RutaArchivo,
                FechaSubida = f.FechaFormato
            }));

            return resultado.OrderByDescending(a => a.FechaSubida).ToList();
        }

        public async Task<List<ConsolidadaDTO>> ObtenerConsolidadasConArchivos(bool? servicio = null, int? idUsuarioMat = null, int? idUsuarioFinan = null)
        {
            // Obtener IDs de consolidadas que tienen archivos
            var archivosQuery = await _repoDiseno.Consultar(f => f.IdConsolidada != null);
            var idsConsolidadasConArchivos = await archivosQuery
                .Select(f => f.IdConsolidada.Value)
                .Distinct()
                .ToListAsync();

            if (idsConsolidadasConArchivos.Count == 0)
                return new List<ConsolidadaDTO>();

            IQueryable<TblConsolidada> query;

            if (idUsuarioMat.HasValue)
            {
                query = await _repoConsolidada.Consultar(c =>
                    c.IdUsuarioMat == idUsuarioMat.Value &&
                    idsConsolidadasConArchivos.Contains(c.ConsolidadaId));
            }
            else
            {
                query = await _repoConsolidada.Consultar(c =>
                    idsConsolidadasConArchivos.Contains(c.ConsolidadaId));
            }

            if (idUsuarioFinan.HasValue)
                query = query.Where(c => c.IdUsuarioFinan == idUsuarioFinan.Value);

            if (servicio.HasValue)
                query = query.Where(c => c.RequiServicio == servicio.Value);

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

        public async Task<bool> AsignarAnalistaConsolidada(int idConsolidada, int idUsuario, int idUsuarioAsignador)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null) return false;

            consolidada.IdUsuarioMat = idUsuario;
            consolidada.IdEstatus = 2;
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            // Propagar a hijas
            var detallesQuery = await _repoDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var detalles = await detallesQuery.ToListAsync();
            var idsHijas = detalles.Select(d => d.IdRequisicion).ToList();

            var requisQuery = await _repoRequisicion.Consultar(r => idsHijas.Contains(r.IdRequisicion));
            var requis = await requisQuery.ToListAsync();

            foreach (var r in requis)
            {
                r.IdUsuarioMat = idUsuario;
                r.IdEstatus = 2;
                r.FechaModificacion = DateTime.Now;
                await _repoRequisicion.Editar(r);

                await _repoBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = r.IdRequisicion,
                    IdEstatus = r.IdEstatus ?? 2,
                    FechaEstatus = DateTime.Now,
                    Observacion = $"[CONSOLIDADA {consolidada.FolioConsolidada}] Asignada a analista.",
                    IdUsuario = idUsuarioAsignador
                });
            }

            return true;
        }
    }
}
