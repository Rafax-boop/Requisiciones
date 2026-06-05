using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Inventario.BLL.Implementacion
{
    public class AlmacenService : IAlmacenService
    {
        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblInventario> _repoInventario;
        private readonly IGenericRepository<TblEstatus> _repoEstatus;
        private readonly IGenericRepository<TblBitacoraEstatus> _repoBitacora;
        private readonly IUnitOfWork _uow;
        private readonly IGenericRepository<TblRequisicionDetalleMovimiento> _repoMovimiento;
        private readonly IGenericRepository<TblFormato> _repoFormato;
        private readonly IEmailService _emailService;
        private readonly IGenericRepository<TblRegistroDiseno> _repoRegistroDiseno;
        private readonly IGenericRepository<TblUsuario> _repoUsuario;
        private readonly IGenericRepository<TblConsolidada> _repoConsolidada;
        private readonly IGenericRepository<TblDepartamento> _repoDepartamento;

        private const int ESTATUS_EN_ALMACEN = 9;
        private const int ESTATUS_APROBADA_ALMACEN = 4;
        private const int ESTATUS_APROBADA_PARCIAL_ALMACEN = 10;
        private const int ESTATUS_RECHAZADA_ALMACEN = 5;
        private const int ESTATUS_EN_COMPRA = 11;
        private const int ESTATUS_ENTREGADO = 12;
        private const string TIPO_COMPRA_BORRADOR = "COMPRA_RECIBIDA";

        public AlmacenService(
            IRequisicionRepository requisicionRepository,
            IGenericRepository<TblInventario> repoInventario,
            IGenericRepository<TblEstatus> repoEstatus,
            IGenericRepository<TblBitacoraEstatus> repoBitacora,
            IUnitOfWork uow,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento,
            IGenericRepository<TblFormato> repoFormato,
            IGenericRepository<TblRegistroDiseno> repoRegistroDiseno,
            IEmailService emailService,
            IGenericRepository<TblUsuario> repoUsuario,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblDepartamento> repoDepartamento)
        {
            _repositoryRequisicion = requisicionRepository;
            _repoInventario = repoInventario;
            _repoEstatus = repoEstatus;
            _repoBitacora = repoBitacora;
            _uow = uow;
            _repoMovimiento = repoMovimiento;
            _repoFormato = repoFormato;
            _repoRegistroDiseno = repoRegistroDiseno;
            _emailService = emailService;
            _repoUsuario = repoUsuario;
            _repoConsolidada = repoConsolidada;
            _repoDepartamento = repoDepartamento;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Helpers privados
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private async Task<TblRequisicion> ObtenerRequisicionConDetallesAsync(int idRequisicion)
        {
            var query = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);
            var req = await query
                .Include(r => r.TblRequisicionDetalles)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .FirstOrDefaultAsync();

            if (req == null)
                throw new Exception($"No se encontró la requisición con ID {idRequisicion}.");

            return req;
        }

        private async Task RegistrarBitacoraAsync(int idRequisicion, int idEstatus, int idUsuario, string observacion)
        {
            await _repoBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = idRequisicion,
                IdEstatus = idEstatus,
                FechaEstatus = DateTime.Now,
                Observacion = observacion.Length > 1000 ? observacion[..1000] : observacion,
                IdUsuario = idUsuario
            });
        }

        private static void ValidarEstatusAlmacen(TblRequisicion req)
        {
            if (req.IdEstatus != ESTATUS_EN_ALMACEN)
                throw new Exception($"La requisición {req.NumRequisicion} no está en estatus de Almacén.");
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Consultas
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen()
        {
            var query = await _repositoryRequisicion.Consultar(r => r.IdEstatus == ESTATUS_EN_ALMACEN);
            return await query
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
                    IdEstatus = r.IdEstatus ?? 0,
                    Estatus = r.IdEstatusNavigation.NombreEstatus,
                    CantidadPartidas = r.TblRequisicionDetalles.Count
                })
                .ToListAsync();
        }

        public async Task<List<RequisicionMaestraDTO>> ListarPedidosAlmacen()
        {
            var movCompraQuery = await _repoMovimiento.Consultar(
                m => m.TipoMovimiento == "COMPRA" && m.Confirmado != true);
            var comprasPorRequi = await movCompraQuery
                .GroupBy(m => m.IdRequisicion)
                .Select(g => new
                {
                    IdRequisicion = g.Key,
                    CantidadPartidasCompra = g.Select(x => x.IdRequisicionDetalle).Distinct().Count()
                })
                .ToListAsync();

            var idsConCompra = comprasPorRequi
                .Select(x => x.IdRequisicion)
                .Distinct()
                .ToList();

            if (!idsConCompra.Any())
                return new List<RequisicionMaestraDTO>();

            var cantidadPartidasCompraPorRequi = comprasPorRequi
                .ToDictionary(x => x.IdRequisicion, x => x.CantidadPartidasCompra);

            var query = await _repositoryRequisicion.Consultar(
                r => r.IdEstatus == 15 && idsConCompra.Contains(r.IdRequisicion));

            var requisiciones = await query
                .Include(r => r.IdDepartamentoNavigation)
                .Include(r => r.IdEstatusNavigation)
                .Include(r => r.Consolidada)
                .OrderByDescending(r => r.FechaModificacion)
                .ToListAsync();

            var individuales = new List<RequisicionMaestraDTO>();
            var consolidadosMap = new Dictionary<int, (TblConsolidada cons, List<TblRequisicion> hijas)>();

            foreach (var r in requisiciones)
            {
                var partidas = cantidadPartidasCompraPorRequi.GetValueOrDefault(r.IdRequisicion, 0);
                if (r.ConsolidadaId == null)
                {
                    individuales.Add(new RequisicionMaestraDTO
                    {
                        IdRequi = r.IdRequisicion,
                        NumRequi = r.NumRequisicion,
                        FechaEmision = r.FechaEmision,
                        FechaModificacion = r.FechaModificacion,
                        Departamento = r.IdDepartamentoNavigation?.NombreDepartamento,
                        Responsable = r.NomResponsableDepartamento,
                        IdEstatus = r.IdEstatus ?? 0,
                        Estatus = r.IdEstatusNavigation?.NombreEstatus,
                        CantidadPartidas = partidas,
                        ConsolidadaId = null,
                        EsConsolidada = false
                    });
                }
                else
                {
                    var cid = r.ConsolidadaId.Value;
                    if (!consolidadosMap.ContainsKey(cid))
                    {
                        consolidadosMap[cid] = (r.Consolidada!, new List<TblRequisicion>());
                    }
                    consolidadosMap[cid].hijas.Add(r);
                }
            }

            var resultado = new List<RequisicionMaestraDTO>();

            foreach (var (cid, (cons, hijas)) in consolidadosMap)
            {
                var totalPartidas = hijas.Sum(h => cantidadPartidasCompraPorRequi.GetValueOrDefault(h.IdRequisicion, 0));
                var deptos = string.Join(", ",
                    hijas.Select(h => h.IdDepartamentoNavigation?.NombreDepartamento)
                         .Where(n => !string.IsNullOrEmpty(n))
                         .Distinct());

                resultado.Add(new RequisicionMaestraDTO
                {
                    IdRequi = cons.ConsolidadaId,
                    NumRequi = cons.FolioConsolidada,
                    FechaEmision = hijas.Min(h => h.FechaEmision),
                    FechaModificacion = hijas.Max(h => h.FechaModificacion),
                    Departamento = deptos,
                    Responsable = hijas.First().NomResponsableDepartamento,
                    IdEstatus = cons.IdEstatus,
                    Estatus = cons.IdEstatusNavigation?.NombreEstatus ?? "En compra",
                    CantidadPartidas = totalPartidas,
                    ConsolidadaId = cons.ConsolidadaId,
                    EsConsolidada = true,
                    IdsRequisiciones = hijas.Select(h => h.IdRequisicion).ToList()
                });
            }

            resultado.AddRange(individuales);
            resultado = resultado.OrderByDescending(r => r.FechaModificacion).ToList();
            return resultado;
        }

        public async Task<List<EntregaPendienteDTO>> ListarEntregasPendientes()
        {
            var movQuery = await _repoMovimiento.Consultar(
                m => m.TipoMovimiento == "ENTREGA" && m.Confirmado != true);

            var movimientos = await movQuery
                .Include(m => m.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            var grupos = movimientos
                .GroupBy(m => m.IdRequisicion)
                .Select(g =>
                {
                    var req = g.First().IdRequisicionNavigation;
                    return new EntregaPendienteDTO
                    {
                        IdRequisicion = g.Key,
                        NumRequi = req?.NumRequisicion ?? "",
                        FechaEmision = req?.FechaEmision,
                        Departamento = req?.IdDepartamentoNavigation?.NombreDepartamento ?? "",
                        Responsable = req?.NomResponsableDepartamento ?? "",
                        Articulos = g.Select(m => new ArticuloEntregaDTO
                        {
                            IdMovimiento = m.IdMovimiento,
                            IdRequisicionDetalle = m.IdRequisicionDetalle,
                            IdArticulo = m.IdRequisicionDetalleNavigation?.IdArticulo,
                            NumPartida = m.IdRequisicionDetalleNavigation?.NumPartida,
                            ClaveMaterial = m.IdRequisicionDetalleNavigation?.IdArticuloNavigation?.Clave ?? "",
                            Descripcion = m.IdRequisicionDetalleNavigation?.Descripcion ?? "",
                            UnidadMedida = m.IdRequisicionDetalleNavigation?.UnidadMedida ?? "",
                            CantidadOriginal = m.CantidadOriginal,
                            CantidadMovimiento = m.CantidadMovimiento,
                            Confirmado = m.Confirmado ?? false
                        }).ToList()
                    };
                })
                .ToList();

            return grupos;
        }

        public async Task<int> GenerarFormatoSalida(int idRequisicion, int idUsuario)
        {
            var queryExistente = await _repoFormato.Consultar(
                f => f.TipoFormato == "SALIDA" && f.IdRequisicion == idRequisicion && f.RutaArchivo == "PENDIENTE");
            var existente = await queryExistente.FirstOrDefaultAsync();
            if (existente != null)
                return existente.NumeroFormato;

            var queryFormatos = await _repoFormato.Consultar(f => f.TipoFormato == "SALIDA");
            var listaFormatos = await queryFormatos.ToListAsync();
            var nuevoNumero = (listaFormatos.Any() ? listaFormatos.Max(f => f.NumeroFormato) : 0) + 1;

            await _repoFormato.Crear(new TblFormato
            {
                NumeroFormato = nuevoNumero,
                TipoFormato = "SALIDA",
                IdRequisicion = idRequisicion,
                FechaFormato = DateTime.Now,
                IdUsuario = idUsuario,
                RutaArchivo = "PENDIENTE"
            });

            return nuevoNumero;
        }

        public async Task<bool> ConfirmarEntrega(int idRequisicion, List<int> idsMovimientos, int idUsuario, string rutaArchivoFirmado)
        {
            if (idsMovimientos == null || idsMovimientos.Count == 0)
                throw new Exception("Debe seleccionar al menos un artículo para confirmar.");

            await _uow.BeginTransactionAsync();
            try
            {
                var queryFormato = await _repoFormato.Consultar(
                    f => f.TipoFormato == "SALIDA" && f.IdRequisicion == idRequisicion && f.RutaArchivo == "PENDIENTE");
                var formato = await queryFormato.FirstOrDefaultAsync()
                    ?? throw new Exception("No se encontró el formato de salida generado para esta requisición.");

                formato.RutaArchivo = rutaArchivoFirmado;
                await _repoFormato.Editar(formato);

                var reqConDetalles = await ObtenerRequisicionConDetallesAsync(idRequisicion);
                var detallesPorId = reqConDetalles.TblRequisicionDetalles
                    .ToDictionary(d => d.IdRequisicionDetalle);

                foreach (var idMov in idsMovimientos)
                {
                    var mov = await _repoMovimiento.Obtener(m => m.IdMovimiento == idMov && m.IdRequisicion == idRequisicion)
                              ?? throw new Exception($"No se encontró el movimiento con ID {idMov}.");

                    var det = detallesPorId.GetValueOrDefault(mov.IdRequisicionDetalle)
                        ?? throw new Exception($"No se encontró el detalle del movimiento {idMov}.");

                    var clave = (det.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (det.Descripcion ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(clave))
                        throw new Exception($"El artículo '{desc}' no tiene clave registrada.");

                    var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                        ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                    if (inv.Existencia < mov.CantidadMovimiento)
                        throw new Exception(
                            $"Stock insuficiente al confirmar entrega de: {desc}. " +
                            $"Disponible: {inv.Existencia}, a entregar: {mov.CantidadMovimiento}.");

                    inv.Existencia -= mov.CantidadMovimiento;
                    await _repoInventario.Editar(inv);

                    mov.Confirmado = true;
                    mov.FechaConfirmacion = DateTime.Now;
                    mov.IdUsuarioConfirmacion = idUsuario;
                    mov.IdFormato = formato.IdFormato;
                    await _repoMovimiento.Editar(mov);
                }

                var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
                if (req != null)
                {
                    var todosEntregasQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == idRequisicion && m.TipoMovimiento == "ENTREGA");
                    var todosEntregas = await todosEntregasQuery.ToListAsync();
                    bool todoConfirmado = todosEntregas.Any() && todosEntregas.All(m => m.Confirmado == true);

                    var mensaje = todoConfirmado
                        ? "Almacén confirmó entrega física de todos los artículos."
                        : $"Almacén confirmó entrega parcial: {idsMovimientos.Count} artículo(s) entregado(s).";
                    await RegistrarBitacoraAsync(idRequisicion, req.IdEstatus ?? 0, idUsuario, mensaje);
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<List<EntregaPendienteDTO>> ListarEntregasPendientesAlmacen()
        {
            var movQuery = await _repoMovimiento.Consultar(
                m => m.TipoMovimiento == "ENTREGA" && m.Confirmado != true);

            var movimientos = await movQuery
                .Include(m => m.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            var gruposPorRequi = movimientos
                .GroupBy(m => m.IdRequisicion)
                .ToList();

            var individuales = new List<EntregaPendienteDTO>();
            var consolidadosData = new Dictionary<int, (TblConsolidada? cons, List<TblRequisicion> reqs, List<TblRequisicionDetalleMovimiento> movs)>();

            foreach (var g in gruposPorRequi)
            {
                var req = g.First().IdRequisicionNavigation;
                if (req?.ConsolidadaId == null)
                {
                    individuales.Add(new EntregaPendienteDTO
                    {
                        IdRequisicion = g.Key,
                        NumRequi = req?.NumRequisicion ?? "",
                        FechaEmision = req?.FechaEmision,
                        Departamento = req?.IdDepartamentoNavigation?.NombreDepartamento ?? "",
                        Responsable = req?.NomResponsableDepartamento ?? "",
                        EsConsolidada = false,
                        Articulos = g.Select(m => new ArticuloEntregaDTO
                        {
                            IdMovimiento = m.IdMovimiento,
                            IdRequisicionDetalle = m.IdRequisicionDetalle,
                            IdArticulo = m.IdRequisicionDetalleNavigation?.IdArticulo,
                            NumPartida = m.IdRequisicionDetalleNavigation?.NumPartida,
                            ClaveMaterial = m.IdRequisicionDetalleNavigation?.IdArticuloNavigation?.Clave ?? "",
                            Descripcion = m.IdRequisicionDetalleNavigation?.Descripcion ?? "",
                            UnidadMedida = m.IdRequisicionDetalleNavigation?.UnidadMedida ?? "",
                            CantidadOriginal = m.CantidadOriginal,
                            CantidadMovimiento = m.CantidadMovimiento,
                            Confirmado = m.Confirmado ?? false
                        }).ToList()
                    });
                }
                else
                {
                    var cid = req.ConsolidadaId.Value;
                    if (!consolidadosData.ContainsKey(cid))
                        consolidadosData[cid] = (req.Consolidada, new List<TblRequisicion>(), new List<TblRequisicionDetalleMovimiento>());
                    var entry = consolidadosData[cid];
                    entry.reqs.Add(req);
                    entry.movs.AddRange(g);
                }
            }

            var cidsOrphan = consolidadosData.Where(kv => kv.Value.cons == null).Select(kv => kv.Key).ToList();
            if (cidsOrphan.Any())
            {
                var consQuery = await _repoConsolidada.Consultar(c => cidsOrphan.Contains(c.ConsolidadaId));
                var consList = await consQuery.ToListAsync();
                foreach (var c in consList)
                {
                    if (consolidadosData.TryGetValue(c.ConsolidadaId, out var entry) && entry.cons == null)
                        consolidadosData[c.ConsolidadaId] = (c, entry.reqs, entry.movs);
                }
            }

            var resultado = new List<EntregaPendienteDTO>(individuales);

            foreach (var (cid, (cons, reqs, movs)) in consolidadosData)
            {
                var idsRequisiciones = reqs.Select(r => r.IdRequisicion).Distinct().ToList();
                var deptos = string.Join(", ",
                    reqs.Select(r => r.IdDepartamentoNavigation?.NombreDepartamento)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .Distinct());
                var fechaMin = reqs.Min(r => r.FechaEmision);

                resultado.Add(new EntregaPendienteDTO
                {
                    IdRequisicion = -cid,
                    ConsolidadaId = cid,
                    NumRequi = cons?.FolioConsolidada ?? $"CONS-{cid}",
                    FechaEmision = fechaMin,
                    Departamento = deptos,
                    Responsable = "CONSOLIDADA",
                    EsConsolidada = true,
                    IdsRequisiciones = idsRequisiciones,
                    Articulos = movs.Select(m => new ArticuloEntregaDTO
                    {
                        IdMovimiento = m.IdMovimiento,
                        IdRequisicionDetalle = m.IdRequisicionDetalle,
                        IdArticulo = m.IdRequisicionDetalleNavigation?.IdArticulo,
                        NumPartida = m.IdRequisicionDetalleNavigation?.NumPartida,
                        ClaveMaterial = m.IdRequisicionDetalleNavigation?.IdArticuloNavigation?.Clave ?? "",
                        Descripcion = m.IdRequisicionDetalleNavigation?.Descripcion ?? "",
                        UnidadMedida = m.IdRequisicionDetalleNavigation?.UnidadMedida ?? "",
                        CantidadOriginal = m.CantidadOriginal,
                        CantidadMovimiento = m.CantidadMovimiento,
                        Confirmado = m.Confirmado ?? false
                    }).ToList()
                });
            }

            return resultado;
        }

        public async Task<int> GenerarFormatoSalidaConsolidada(int idConsolidada, int idUsuario)
        {
            var queryExistente = await _repoFormato.Consultar(
                f => f.TipoFormato == "SALIDA" && f.IdConsolidada == idConsolidada && f.RutaArchivo == "PENDIENTE");
            var existente = await queryExistente.FirstOrDefaultAsync();
            if (existente != null)
                return existente.NumeroFormato;

            var queryFormatos = await _repoFormato.Consultar(f => f.TipoFormato == "SALIDA");
            var listaFormatos = await queryFormatos.ToListAsync();
            var nuevoNumero = (listaFormatos.Any() ? listaFormatos.Max(f => f.NumeroFormato) : 0) + 1;

            await _repoFormato.Crear(new TblFormato
            {
                NumeroFormato = nuevoNumero,
                TipoFormato = "SALIDA",
                IdConsolidada = idConsolidada,
                FechaFormato = DateTime.Now,
                IdUsuario = idUsuario,
                RutaArchivo = "PENDIENTE"
            });

            return nuevoNumero;
        }

        public async Task<bool> ConfirmarEntregaConsolidada(int idConsolidada, List<int> idsMovimientos, int idUsuario, string rutaArchivoFirmado)
        {
            if (idsMovimientos == null || idsMovimientos.Count == 0)
                throw new Exception("Debe seleccionar al menos un artículo para confirmar.");

            var childQuery = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var children = await childQuery.ToListAsync();
            if (!children.Any())
                throw new Exception("No se encontraron requisiciones hijas para esta consolidada.");

            var childIds = children.Select(c => c.IdRequisicion).ToHashSet();

            await _uow.BeginTransactionAsync();
            try
            {
                var queryFormato = await _repoFormato.Consultar(
                    f => f.TipoFormato == "SALIDA" && f.IdConsolidada == idConsolidada && f.RutaArchivo == "PENDIENTE");
                var formato = await queryFormato.FirstOrDefaultAsync()
                    ?? throw new Exception("No se encontró el formato de salida generado para esta consolidada.");

                formato.RutaArchivo = rutaArchivoFirmado;
                await _repoFormato.Editar(formato);

                var movsPorChild = idsMovimientos
                    .GroupBy(idMov => idMov)
                    .ToDictionary(g => g.Key, g => g.Key);

                var allMovsQuery = await _repoMovimiento.Consultar(
                    m => idsMovimientos.Contains(m.IdMovimiento) && childIds.Contains(m.IdRequisicion));
                var allMovs = await allMovsQuery.ToListAsync();

                if (allMovs.Count != idsMovimientos.Count)
                    throw new Exception("Algunos movimientos no pertenecen a las requisiciones de esta consolidada.");

                foreach (var child in children)
                {
                    var childMovs = allMovs.Where(m => m.IdRequisicion == child.IdRequisicion).ToList();
                    if (!childMovs.Any()) continue;

                    var childConDetalles = await ObtenerRequisicionConDetallesAsync(child.IdRequisicion);
                    var detallesPorId = childConDetalles.TblRequisicionDetalles
                        .ToDictionary(d => d.IdRequisicionDetalle);

                    foreach (var mov in childMovs)
                    {
                        var det = detallesPorId.GetValueOrDefault(mov.IdRequisicionDetalle)
                            ?? throw new Exception($"No se encontró el detalle del movimiento {mov.IdMovimiento} en la requisición {child.IdRequisicion}.");

                        var clave = (det.IdArticuloNavigation?.Clave ?? "").Trim();
                        var desc = (det.Descripcion ?? "").Trim();

                        if (string.IsNullOrWhiteSpace(clave))
                            throw new Exception($"El artículo '{desc}' no tiene clave registrada.");

                        var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                            ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                        if (inv.Existencia < mov.CantidadMovimiento)
                            throw new Exception(
                                $"Stock insuficiente al confirmar entrega de: {desc}. " +
                                $"Disponible: {inv.Existencia}, a entregar: {mov.CantidadMovimiento}.");

                        inv.Existencia -= mov.CantidadMovimiento;
                        await _repoInventario.Editar(inv);

                        mov.Confirmado = true;
                        mov.FechaConfirmacion = DateTime.Now;
                        mov.IdUsuarioConfirmacion = idUsuario;
                        mov.IdFormato = formato.IdFormato;
                        await _repoMovimiento.Editar(mov);
                    }

                    var req = children.First(r => r.IdRequisicion == child.IdRequisicion);
                    var todosEntregasQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == child.IdRequisicion && m.TipoMovimiento == "ENTREGA");
                    var todosEntregas = await todosEntregasQuery.ToListAsync();
                    bool todoConfirmado = todosEntregas.Any() && todosEntregas.All(m => m.Confirmado == true);

                    var mensaje = todoConfirmado
                        ? "Almacén confirmó entrega física de todos los artículos (consolidada)."
                        : $"Almacén confirmó entrega parcial en consolidada: {childMovs.Count} artículo(s) entregado(s).";
                    await RegistrarBitacoraAsync(child.IdRequisicion, req.IdEstatus ?? 0, idUsuario, mensaje);
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<List<string>> ObtenerEstatus()
        {
            var query = await _repoEstatus.Consultar();
            var entities = await query.ToListAsync();
            return entities
                .Select(e => e.NombreEstatus)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        public async Task<List<InventarioItemDTO>> ObtenerInventario()
        {
            var query = await _repoInventario.Consultar();
            var entities = await query.ToListAsync();
            return entities.Select(i => new InventarioItemDTO
            {
                Clave = i.Clave,
                Descripcion = i.Descripcion ?? "",
                UnidadMedida = i.UnidadMedida ?? "",
                Existencia = i.Existencia,
                Minimo = 0,
                Situacion = i.Existencia == 0 ? "Sin stock" : "OK"
            }).ToList();
        }

        public async Task<bool> RegistrarIngresoInventarioLote(List<IngresoInventarioDTO> items)
        {
            if (items == null || !items.Any())
                throw new Exception("Debe agregar al menos un artículo.");

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var dto in items)
                {
                    var descripcion  = (dto.Descripcion ?? "").Trim();
                    var unidadMedida = (dto.UnidadMedida ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(descripcion))  throw new Exception("La descripción es obligatoria.");
                    if (string.IsNullOrWhiteSpace(unidadMedida)) throw new Exception("La unidad de medida es obligatoria.");
                    if (dto.Cantidad <= 0) throw new Exception("La cantidad debe ser mayor a 0.");

                    var inv = await _repoInventario.Obtener(i => i.Descripcion == descripcion && i.UnidadMedida == unidadMedida);

                    if (inv == null)
                    {
                        if (string.IsNullOrWhiteSpace(dto.Clave))
                            throw new Exception($"La clave es obligatoria para el material nuevo '{descripcion}'.");

                        inv = await _repoInventario.Crear(new TblInventario
                        {
                            Clave         = dto.Clave.Trim(),
                            Descripcion   = descripcion,
                            UnidadMedida  = unidadMedida,
                            Entrada       = 0,
                            Existencia    = 0,
                            Costo         = 0,
                            Iva           = 0,
                            CostoUnitario = 0,
                            Total         = 0
                        });
                    }

                    inv!.Existencia += dto.Cantidad;
                    await _repoInventario.Editar(inv);
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<List<StockPartidasDTO>> ConsultarStockParaRequisicion(int idRequisicion)
        {
            var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);
            var resultado = new List<StockPartidasDTO>();

            foreach (var d in req.TblRequisicionDetalles)
            {
                var clave = (d.IdArticuloNavigation?.Clave ?? "").Trim();
                var desc = (d.Descripcion ?? "").Trim();
                var unidad = (d.UnidadMedida ?? "").Trim();
                var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);
                var inv = await _repoInventario.Obtener(i => i.Clave == clave);

                resultado.Add(new StockPartidasDTO
                {
                    IdRequisicionDetalle = d.IdRequisicionDetalle,
                    Descripcion = desc,
                    UnidadMedida = unidad,
                    CantidadSolicitada = cantSolicitada,
                    StockDisponible = inv?.Existencia ?? 0,
                    ExisteEnInventario = inv != null
                });
            }

            return resultado;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Operaciones con transacción
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<bool> RegistrarIngresoInventario(IngresoInventarioDTO dto)
        {
            var descripcion = (dto.Descripcion ?? "").Trim();
            var unidadMedida = (dto.UnidadMedida ?? "").Trim();
            var motivo = (dto.Motivo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(descripcion)) throw new Exception("La descripción es obligatoria.");
            if (string.IsNullOrWhiteSpace(unidadMedida)) throw new Exception("La unidad de medida es obligatoria.");
            if (dto.Cantidad <= 0) throw new Exception("La cantidad debe ser mayor a 0.");
            if (string.IsNullOrWhiteSpace(motivo)) throw new Exception("El motivo es obligatorio.");

            await _uow.BeginTransactionAsync();
            try
            {
                var inv = await _repoInventario.Obtener(i => i.Descripcion == descripcion && i.UnidadMedida == unidadMedida);
                bool esNuevo = inv == null;

                if (esNuevo)
                {
                    if (string.IsNullOrWhiteSpace(dto.Clave))
                        throw new Exception("La clave es obligatoria para dar de alta un material nuevo.");

                    inv = await _repoInventario.Crear(new TblInventario
                    {
                        Clave = dto.Clave.Trim(),
                        Descripcion = descripcion,
                        UnidadMedida = unidadMedida,
                        Entrada = 0,
                        Existencia = 0,
                        Costo = 0,
                        Iva = 0,
                        CostoUnitario = 0,
                        Total = 0
                    });
                }

                inv!.Existencia += dto.Cantidad;
                await _repoInventario.Editar(inv);

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo)
        {
            motivo = (motivo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(motivo))
                throw new Exception("El motivo de rechazo es obligatorio.");

            await _uow.BeginTransactionAsync();
            try
            {
                var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion)
                          ?? throw new Exception($"No se encontró la requisición con ID {idRequisicion}.");

                ValidarEstatusAlmacen(req);

                req.IdEstatus = ESTATUS_RECHAZADA_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                await RegistrarBitacoraAsync(req.IdRequisicion, ESTATUS_RECHAZADA_ALMACEN, idUsuario,
                    $"Almacén rechazó requisición. Motivo: {motivo}");

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> ProcesarRequisicion(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> entregas,
            IEnumerable<(int idRequisicionDetalle, int cantidadComprar)> compras,
            bool enviarCorreo = true)
        {
            var listaEntregas = (entregas ?? Enumerable.Empty<(int, int)>()).ToList();
            var listaCompras = (compras ?? Enumerable.Empty<(int, int)>()).ToList();

            if (listaEntregas.Count == 0 && listaCompras.Count == 0)
                throw new Exception("Debe indicar al menos una entrega o una compra.");
            if (listaEntregas.Any(x => x.cantidadAprobada < 0))
                throw new Exception("Las cantidades de entrega no pueden ser negativas.");
            if (listaCompras.Any(x => x.cantidadComprar <= 0))
                throw new Exception("Las cantidades de compra deben ser mayores a 0.");

            await _uow.BeginTransactionAsync();
            try
            {
                var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);
                ValidarEstatusAlmacen(req);

                var detallesById = req.TblRequisicionDetalles.ToDictionary(d => d.IdRequisicionDetalle);
                var resumenEntregas = new List<string>();
                var resumenCompras = new List<string>();

                foreach (var (idDetalle, cantAprobada) in listaEntregas)
                {
                    if (cantAprobada <= 0) continue;

                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        throw new Exception("Una de las partidas de entrega no pertenece a la requisición.");

                    var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);
                    if (cantAprobada > cantSolicitada)
                        throw new Exception($"La cantidad aprobada ({cantAprobada}) no puede exceder la solicitada ({cantSolicitada}).");

                    var clave = (d.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (d.Descripcion ?? "").Trim();
                    var unidad = (d.UnidadMedida ?? "").Trim();
                    var cantRecibida = cantAprobada;

                    if (string.IsNullOrWhiteSpace(clave))
                        throw new Exception($"El artículo '{desc}' no tiene clave registrada.");

                    var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                              ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                    if (inv.Existencia < cantAprobada)
                        throw new Exception($"Stock insuficiente para: {desc}. Disponible: {inv.Existencia}, aprobado: {cantAprobada}.");

                    await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                    {
                        IdRequisicion = idRequisicion,
                        IdRequisicionDetalle = idDetalle,
                        TipoMovimiento = "ENTREGA",
                        CantidadOriginal = cantSolicitada,
                        CantidadMovimiento = cantAprobada,
                        FechaMovimiento = DateTime.Now,
                        IdUsuario = idUsuario,
                        Confirmado = false
                    });

                    resumenEntregas.Add($"{desc} â€” {cantAprobada} {unidad}");
                }

                foreach (var (idDetalle, cantComprar) in listaCompras)
                {
                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        throw new Exception("Una de las partidas de compra no pertenece a la requisición.");

                    var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);

                    await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                    {
                        IdRequisicion = idRequisicion,
                        IdRequisicionDetalle = idDetalle,
                        TipoMovimiento = "COMPRA",
                        CantidadOriginal = cantSolicitada,
                        CantidadMovimiento = cantComprar,
                        FechaMovimiento = DateTime.Now,
                        IdUsuario = idUsuario,
                        Confirmado = false
                    });

                    resumenCompras.Add($"{(d.Descripcion ?? "").Trim()} x{cantComprar}");
                }

                int estatusFinal = listaCompras.Count > 0 ? ESTATUS_EN_COMPRA : ESTATUS_APROBADA_ALMACEN;

                req.IdEstatus = estatusFinal;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                var obs = new StringBuilder("Almacén procesó requisición.");
                if (resumenEntregas.Count > 0) obs.Append($" Preparados para entrega: {string.Join(", ", resumenEntregas)}.");
                if (resumenCompras.Count > 0) obs.Append($" Enviados a compra: {string.Join(", ", resumenCompras)}.");

                await RegistrarBitacoraAsync(req.IdRequisicion, estatusFinal, idUsuario, obs.ToString());

                if (resumenEntregas.Count > 0 && enviarCorreo)
                {
                    // Ajusta req.CorreoResponsable al campo real de tu entidad
                    var correo = req.Correo ?? "";
                    if (!string.IsNullOrWhiteSpace(correo))
                    {
                        try
                        {
                            await _emailService.NotificarProductosListosEntregaAsync(
                                correo,
                                req.NumRequisicion ?? "",
                                req.IdDepartamentoNavigation?.NombreDepartamento ?? "",
                                req.NomResponsableDepartamento ?? "",
                                resumenEntregas,
                                resumenCompras);
                        }
                        catch (Exception exMail)
                        {
                            // El correo nunca debe tumbar la transacción principal
                            // Puedes loggear aquí con ILogger si lo tienes inyectado
                            _ = exMail;
                        }
                    }
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GenerarFormatoEntrada(int idRequisicion, int idUsuario)
        {
            var queryExistente = await _repoFormato.Consultar(
                f => f.TipoFormato == "ENTRADA" && f.IdRequisicion == idRequisicion && f.RutaArchivo == "PENDIENTE");
            var existente = await queryExistente.FirstOrDefaultAsync();
            if (existente != null)
                return existente.NumeroFormato;

            var queryFormatos = await _repoFormato.Consultar(f => f.TipoFormato == "ENTRADA");
            var listaFormatos = await queryFormatos.ToListAsync();
            var nuevoNumero = (listaFormatos.Any() ? listaFormatos.Max(f => f.NumeroFormato) : 0) + 1;

            await _repoFormato.Crear(new TblFormato
            {
                NumeroFormato = nuevoNumero,
                TipoFormato = "ENTRADA",
                IdRequisicion = idRequisicion,
                FechaFormato = DateTime.Now,
                IdUsuario = idUsuario,
                RutaArchivo = "PENDIENTE"
            });

            return nuevoNumero;
        }

        public async Task<List<PartidaCompraEntradaDTO>> ObtenerPartidasCompraParaEntrada(int idRequisicion)
        {
            var query = await _repoMovimiento.Consultar(
                m => m.IdRequisicion == idRequisicion
                  && m.TipoMovimiento == "COMPRA"
                  && m.Confirmado != true);

            var movimientosCompra = await query
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            var partidas = movimientosCompra
                .GroupBy(m => m.IdRequisicionDetalle)
                .Select(g =>
                {
                    var det = g.First().IdRequisicionDetalleNavigation;
                    return new PartidaCompraEntradaDTO
                    {
                        IdRequisicionDetalle = g.Key,
                        NumPartida = det?.NumPartida,
                        IdArticulo = det?.IdArticulo,
                        ClaveMaterial = det?.IdArticuloNavigation?.Clave ?? string.Empty,
                        Descripcion = det?.Descripcion ?? string.Empty,
                        UnidadMedida = det?.UnidadMedida ?? string.Empty,
                        CantidadComprar = g.Sum(x => (decimal)x.CantidadMovimiento)
                    };
                })
                .OrderBy(x => x.NumPartida)
                .ThenBy(x => x.IdRequisicionDetalle)
                .ToList();

            return partidas;
        }

        public async Task GuardarBorradorIngreso(int idRequisicion, int idUsuario, List<CantidadRecibidaDTO> cantidades)
        {
            if (cantidades == null || cantidades.Count == 0)
                throw new Exception("Debe indicar al menos una cantidad recibida.");

            if (cantidades.Any(c => c.CantidadRecibida < 0))
                throw new Exception("Las cantidades recibidas no pueden ser negativas.");

            await _uow.BeginTransactionAsync();
            try
            {
                var borradorQuery = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion
                      && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                      && m.Confirmado != true);
                var borradorPrevio = await borradorQuery.ToListAsync();
                foreach (var b in borradorPrevio)
                    await _repoMovimiento.Eliminar(b);

                var compraQuery = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion
                      && m.TipoMovimiento == "COMPRA"
                      && m.Confirmado != true);
                var movsCompra = await compraQuery
                    .Include(m => m.IdRequisicionDetalleNavigation)
                    .ToListAsync();

                if (!movsCompra.Any())
                    throw new Exception("No se encontraron partidas de compra pendientes.");

                var movsPorDetalle = movsCompra
                    .GroupBy(m => m.IdRequisicionDetalle)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var item in cantidades)
                {
                    if (!movsPorDetalle.TryGetValue(item.IdRequisicionDetalle, out var movsDetalle))
                        continue;

                    var cantSolicitada = movsDetalle.Sum(m => (decimal)m.CantidadMovimiento);
                    var cantRecibida = Math.Min(item.CantidadRecibida, cantSolicitada);

                    if (cantRecibida <= 0) continue;

                    await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                    {
                        IdRequisicion = idRequisicion,
                        IdRequisicionDetalle = item.IdRequisicionDetalle,
                        TipoMovimiento = TIPO_COMPRA_BORRADOR,
                        CantidadOriginal = (int)cantSolicitada,
                        CantidadMovimiento = (int)cantRecibida,
                        FechaMovimiento = DateTime.Now,
                        IdUsuario = idUsuario,
                        Confirmado = false,
                        Observacion = "Borrador â€” pendiente de confirmar con formato firmado"
                    });
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PartidaCompraEntradaDTO>> ObtenerBorradorIngreso(int idRequisicion)
        {
            var query = await _repoMovimiento.Consultar(
                m => m.IdRequisicion == idRequisicion
                  && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                  && m.Confirmado != true);

            var movs = await query
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            return movs
                .GroupBy(m => m.IdRequisicionDetalle)
                .Select(g =>
                {
                    var det = g.First().IdRequisicionDetalleNavigation;
                    return new PartidaCompraEntradaDTO
                    {
                        IdRequisicionDetalle = g.Key,
                        NumPartida = det?.NumPartida,
                        IdArticulo = det?.IdArticulo,
                        ClaveMaterial = det?.IdArticuloNavigation?.Clave ?? "",
                        Descripcion = det?.Descripcion ?? "",
                        UnidadMedida = det?.UnidadMedida ?? "",
                        CantidadComprar = g.Sum(x => (decimal)x.CantidadMovimiento)
                    };
                })
                .OrderBy(x => x.NumPartida)
                .ThenBy(x => x.IdRequisicionDetalle)
                .ToList();
        }

        public async Task<bool> ConfirmarIngresoPedido(int idRequisicion, int idUsuario, string rutaArchivoFirmado, bool enviarCorreo = true)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                var borradorQuery = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion
                      && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                      && m.Confirmado != true);
                var borradores = await borradorQuery
                    .Include(m => m.IdRequisicionDetalleNavigation)
                    .ToListAsync();

                if (!borradores.Any())
                    throw new Exception("No hay borrador guardado. Captura las cantidades recibidas primero.");

                var borradorPorDetalle = borradores
                    .GroupBy(m => m.IdRequisicionDetalle)
                    .ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.CantidadMovimiento));

                // â”€â”€ Leer TODAS las compras originales pendientes â”€â”€
                var compraQuery = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion
                      && m.TipoMovimiento == "COMPRA"
                      && m.Confirmado != true);
                var movsCompra = await compraQuery
                    .Include(m => m.IdRequisicionDetalleNavigation)
                        .ThenInclude(d => d.IdArticuloNavigation)
                    .ToListAsync();

                if (!movsCompra.Any())
                    throw new Exception("No se encontraron partidas de compra pendientes.");

                var fmtQuery = await _repoFormato.Consultar(
                    f => f.TipoFormato == "ENTRADA"
                      && f.IdRequisicion == idRequisicion
                      && f.RutaArchivo == "PENDIENTE");
                var formato = await fmtQuery.FirstOrDefaultAsync();
                if (formato != null)
                {
                    formato.RutaArchivo = rutaArchivoFirmado;
                    await _repoFormato.Editar(formato);
                }

                var resumenEntregas = new List<string>();
                var resumenFaltantes = new List<string>();
                bool hayFaltante = false;

                // Agrupar compras originales por detalle
                var movsPorDetalle = movsCompra
                    .GroupBy(m => m.IdRequisicionDetalle)
                    .ToDictionary(g => g.Key, g => g.ToList());

                foreach (var (idDetalle, movsOriginales) in movsPorDetalle)
                {
                    var det = movsOriginales.First().IdRequisicionDetalleNavigation;
                    var desc = (det?.Descripcion ?? "").Trim();
                    var clave = (det?.IdArticuloNavigation?.Clave ?? "").Trim();
                    var unidad = (det?.UnidadMedida ?? "").Trim();
                    var cantSolicitada = movsOriginales.Sum(m => (decimal)m.CantidadMovimiento);
                    var cantRecibida = borradorPorDetalle.GetValueOrDefault(idDetalle, 0);
                    var cantFaltante = cantSolicitada - cantRecibida;

                    // â”€â”€ Marcar TODOS los movimientos COMPRA originales como confirmados â”€â”€
                    foreach (var mov in movsOriginales)
                    {
                        mov.Confirmado = true;
                        mov.FechaConfirmacion = DateTime.Now;
                        mov.IdUsuarioConfirmacion = idUsuario;
                        await _repoMovimiento.Editar(mov);
                    }

                    // â”€â”€ Lo recibido â†’ ENTREGA pendiente + actualizar inventario â”€â”€
                    if (cantRecibida > 0)
                    {
                        await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                        {
                            IdRequisicion = idRequisicion,
                            IdRequisicionDetalle = idDetalle,
                            TipoMovimiento = "ENTREGA",
                            CantidadOriginal = (int)cantSolicitada,
                            CantidadMovimiento = (int)cantRecibida,
                            FechaMovimiento = DateTime.Now,
                            IdUsuario = idUsuario,
                            Confirmado = false
                        });
                        resumenEntregas.Add($"{desc} â€” {cantRecibida} {unidad}");

                        // â”€â”€ Actualizar o crear en inventario â”€â”€
                        if (!string.IsNullOrWhiteSpace(clave))
                        {
                            var inv = await _repoInventario.Obtener(i => i.Clave == clave);
                            if (inv != null)
                            {
                                inv.Existencia += (int)cantRecibida;
                                await _repoInventario.Editar(inv);
                            }
                            else
                            {
                                await _repoInventario.Crear(new TblInventario
                                {
                                    Clave = clave,
                                    Descripcion = desc,
                                    UnidadMedida = unidad,
                                    Existencia = (int)cantRecibida,
                                    Entrada = (int)cantRecibida,
                                    Costo = 0,
                                    Iva = 0,
                                    CostoUnitario = 0,
                                    Total = 0
                                });
                            }
                        }
                    }

                    // â”€â”€ Faltante â†’ nuevo COMPRA pendiente (queda en Pedidos) â”€â”€
                    if (cantFaltante > 0)
                    {
                        hayFaltante = true;
                        await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                        {
                            IdRequisicion = idRequisicion,
                            IdRequisicionDetalle = idDetalle,
                            TipoMovimiento = "COMPRA",
                            CantidadOriginal = (int)cantSolicitada,
                            CantidadMovimiento = (int)cantFaltante,
                            FechaMovimiento = DateTime.Now,
                            IdUsuario = idUsuario,
                            Confirmado = false,
                            Observacion = $"Faltante del proveedor â€” entrega anterior: {cantRecibida}"
                        });
                        resumenFaltantes.Add($"{desc} faltante: x{cantFaltante}");
                    }
                }

                // â”€â”€ Eliminar borradores ya procesados â”€â”€
                foreach (var b in borradores)
                    await _repoMovimiento.Eliminar(b);

                var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion)
                          ?? throw new Exception("No se encontró la requisición.");

                var obs = new StringBuilder("Almacén registró ingreso de material del proveedor.");
                if (resumenEntregas.Count > 0) obs.Append($" Preparado para entrega: {string.Join(", ", resumenEntregas)}.");
                if (resumenFaltantes.Count > 0) obs.Append($" Pendiente del proveedor: {string.Join(", ", resumenFaltantes)}.");

                await RegistrarBitacoraAsync(idRequisicion, req.IdEstatus ?? 7, idUsuario, obs.ToString());

                var usuario = await _repoUsuario.Obtener(u => u.IdUsuario == req.IdUsuario);
                if (enviarCorreo)
                {
                    var correoResponsable = usuario?.Correo ?? "";
                    if (!string.IsNullOrWhiteSpace(correoResponsable))
                    {
                        await _emailService.NotificarPedidoRecibidoParcialAsync(
                            correoResponsable,
                            req.NumRequisicion ?? "",
                            resumenEntregas,
                            resumenFaltantes);
                    }
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Entradas â€” Consolidada
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public async Task<List<PartidaCompraEntradaDTO>> ObtenerPartidasCompraConsolidada(int idConsolidada)
        {
            var childQuery = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var childIds = await childQuery.Select(r => r.IdRequisicion).ToListAsync();
            if (!childIds.Any()) return new List<PartidaCompraEntradaDTO>();

            var query = await _repoMovimiento.Consultar(
                m => childIds.Contains(m.IdRequisicion)
                  && m.TipoMovimiento == "COMPRA"
                  && m.Confirmado != true);

            var movimientosCompra = await query
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            return movimientosCompra
                .GroupBy(m => m.IdRequisicionDetalle)
                .Select(g =>
                {
                    var det = g.First().IdRequisicionDetalleNavigation;
                    return new PartidaCompraEntradaDTO
                    {
                        IdRequisicionDetalle = g.Key,
                        IdRequisicion = g.First().IdRequisicion,
                        NumPartida = det?.NumPartida,
                        IdArticulo = det?.IdArticulo,
                        ClaveMaterial = det?.IdArticuloNavigation?.Clave ?? string.Empty,
                        Descripcion = det?.Descripcion ?? string.Empty,
                        UnidadMedida = det?.UnidadMedida ?? string.Empty,
                        CantidadComprar = g.Sum(x => (decimal)x.CantidadMovimiento)
                    };
                })
                .OrderBy(x => x.IdRequisicion)
                .ThenBy(x => x.NumPartida)
                .ThenBy(x => x.IdRequisicionDetalle)
                .ToList();
        }

        public async Task GuardarBorradorIngresoConsolidada(int idConsolidada, int idUsuario, List<CantidadRecibidaDTO> cantidades)
        {
            var childQuery = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var childIds = await childQuery.Select(r => r.IdRequisicion).ToListAsync();
            if (!childIds.Any())
                throw new Exception("No se encontraron requisiciones hijas para la consolidada.");

            await _uow.BeginTransactionAsync();
            try
            {
                foreach (var childId in childIds)
                {
                    var compraQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == childId
                          && m.TipoMovimiento == "COMPRA"
                          && m.Confirmado != true);
                    var childCompraDetalles = await compraQuery
                        .Select(m => m.IdRequisicionDetalle)
                        .Distinct()
                        .ToListAsync();

                    if (!childCompraDetalles.Any()) continue;

                    var borradorQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == childId
                          && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                          && m.Confirmado != true);
                    var borradorPrevio = await borradorQuery.ToListAsync();
                    foreach (var b in borradorPrevio)
                        await _repoMovimiento.Eliminar(b);

                    var compraConDetalles = await compraQuery
                        .Include(m => m.IdRequisicionDetalleNavigation)
                        .ToListAsync();

                    var movsPorDetalle = compraConDetalles
                        .GroupBy(m => m.IdRequisicionDetalle)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var item in cantidades.Where(c => childCompraDetalles.Contains(c.IdRequisicionDetalle)))
                    {
                        if (!movsPorDetalle.TryGetValue(item.IdRequisicionDetalle, out var movsDetalle))
                            continue;

                        var cantSolicitada = movsDetalle.Sum(m => (decimal)m.CantidadMovimiento);
                        var cantRecibida = Math.Min(item.CantidadRecibida, cantSolicitada);
                        if (cantRecibida <= 0) continue;

                        await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                        {
                            IdRequisicion = childId,
                            IdRequisicionDetalle = item.IdRequisicionDetalle,
                            TipoMovimiento = TIPO_COMPRA_BORRADOR,
                            CantidadOriginal = (int)cantSolicitada,
                            CantidadMovimiento = (int)cantRecibida,
                            FechaMovimiento = DateTime.Now,
                            IdUsuario = idUsuario,
                            Confirmado = false,
                            Observacion = "Borrador consolidado â€” pendiente de confirmar con formato firmado"
                        });
                    }
                }

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<int> GenerarFormatoEntradaConsolidada(int idConsolidada, int idUsuario)
        {
            var queryExistente = await _repoFormato.Consultar(
                f => f.TipoFormato == "ENTRADA" && f.IdConsolidada == idConsolidada && f.RutaArchivo == "PENDIENTE");
            var existente = await queryExistente.FirstOrDefaultAsync();
            if (existente != null)
                return existente.NumeroFormato;

            var queryFormatos = await _repoFormato.Consultar(f => f.TipoFormato == "ENTRADA");
            var listaFormatos = await queryFormatos.ToListAsync();
            var nuevoNumero = (listaFormatos.Any() ? listaFormatos.Max(f => f.NumeroFormato) : 0) + 1;

            await _repoFormato.Crear(new TblFormato
            {
                NumeroFormato = nuevoNumero,
                TipoFormato = "ENTRADA",
                IdConsolidada = idConsolidada,
                FechaFormato = DateTime.Now,
                IdUsuario = idUsuario,
                RutaArchivo = "PENDIENTE"
            });

            return nuevoNumero;
        }

        public async Task<List<PartidaCompraEntradaDTO>> ObtenerBorradorIngresoConsolidada(int idConsolidada)
        {
            var childQuery = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var childIds = await childQuery.Select(r => r.IdRequisicion).ToListAsync();
            if (!childIds.Any()) return new List<PartidaCompraEntradaDTO>();

            var query = await _repoMovimiento.Consultar(
                m => childIds.Contains(m.IdRequisicion)
                  && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                  && m.Confirmado != true);

            var movs = await query
                .Include(m => m.IdRequisicionDetalleNavigation)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .ToListAsync();

            return movs
                .GroupBy(m => m.IdRequisicionDetalle)
                .Select(g =>
                {
                    var det = g.First().IdRequisicionDetalleNavigation;
                    return new PartidaCompraEntradaDTO
                    {
                        IdRequisicionDetalle = g.Key,
                        IdRequisicion = g.First().IdRequisicion,
                        NumPartida = det?.NumPartida,
                        IdArticulo = det?.IdArticulo,
                        ClaveMaterial = det?.IdArticuloNavigation?.Clave ?? "",
                        Descripcion = det?.Descripcion ?? "",
                        UnidadMedida = det?.UnidadMedida ?? "",
                        CantidadComprar = g.Sum(x => (decimal)x.CantidadMovimiento)
                    };
                })
                .OrderBy(x => x.IdRequisicion)
                .ThenBy(x => x.NumPartida)
                .ThenBy(x => x.IdRequisicionDetalle)
                .ToList();
        }

        public async Task<bool> ConfirmarIngresoPedidoConsolidada(int idConsolidada, int idUsuario, string rutaArchivoFirmado)
        {
            var childQuery = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            var childIds = await childQuery.Select(r => r.IdRequisicion).ToListAsync();
            if (!childIds.Any())
                throw new Exception("No se encontraron requisiciones hijas para la consolidada.");

            await _uow.BeginTransactionAsync();
            try
            {
                var fmtQuery = await _repoFormato.Consultar(
                    f => f.TipoFormato == "ENTRADA"
                      && f.IdConsolidada == idConsolidada
                      && f.RutaArchivo == "PENDIENTE");
                var formato = await fmtQuery.FirstOrDefaultAsync();
                if (formato != null)
                {
                    formato.RutaArchivo = rutaArchivoFirmado;
                    await _repoFormato.Editar(formato);
                }

                bool hayFaltanteGlobal = false;

                foreach (var childId in childIds)
                {
                    var borradorQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == childId
                          && m.TipoMovimiento == TIPO_COMPRA_BORRADOR
                          && m.Confirmado != true);
                    var borradores = await borradorQuery
                        .Include(m => m.IdRequisicionDetalleNavigation)
                        .ToListAsync();

                    if (!borradores.Any()) continue;

                    var borradorPorDetalle = borradores
                        .GroupBy(m => m.IdRequisicionDetalle)
                        .ToDictionary(g => g.Key, g => g.Sum(x => (decimal)x.CantidadMovimiento));

                    var compraQuery = await _repoMovimiento.Consultar(
                        m => m.IdRequisicion == childId
                          && m.TipoMovimiento == "COMPRA"
                          && m.Confirmado != true);
                    var movsCompra = await compraQuery
                        .Include(m => m.IdRequisicionDetalleNavigation)
                            .ThenInclude(d => d.IdArticuloNavigation)
                        .ToListAsync();

                    if (!movsCompra.Any()) continue;

                    var resumenEntregas = new List<string>();
                    var resumenFaltantes = new List<string>();
                    bool hayFaltante = false;

                    var movsPorDetalle = movsCompra
                        .GroupBy(m => m.IdRequisicionDetalle)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    foreach (var (idDetalle, movsOriginales) in movsPorDetalle)
                    {
                        var det = movsOriginales.First().IdRequisicionDetalleNavigation;
                        var desc = (det?.Descripcion ?? "").Trim();
                        var clave = (det?.IdArticuloNavigation?.Clave ?? "").Trim();
                        var unidad = (det?.UnidadMedida ?? "").Trim();
                        var cantSolicitada = movsOriginales.Sum(m => (decimal)m.CantidadMovimiento);
                        var cantRecibida = borradorPorDetalle.GetValueOrDefault(idDetalle, 0);
                        var cantFaltante = cantSolicitada - cantRecibida;

                        foreach (var mov in movsOriginales)
                        {
                            mov.Confirmado = true;
                            mov.FechaConfirmacion = DateTime.Now;
                            mov.IdUsuarioConfirmacion = idUsuario;
                            await _repoMovimiento.Editar(mov);
                        }

                        if (cantRecibida > 0)
                        {
                            await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                            {
                                IdRequisicion = childId,
                                IdRequisicionDetalle = idDetalle,
                                TipoMovimiento = "ENTREGA",
                                CantidadOriginal = (int)cantSolicitada,
                                CantidadMovimiento = (int)cantRecibida,
                                FechaMovimiento = DateTime.Now,
                                IdUsuario = idUsuario,
                                Confirmado = false
                            });
                            resumenEntregas.Add($"{desc} â€” {cantRecibida} {unidad}");

                            if (!string.IsNullOrWhiteSpace(clave))
                            {
                                var inv = await _repoInventario.Obtener(i => i.Clave == clave);
                                if (inv != null)
                                {
                                    inv.Existencia += (int)cantRecibida;
                                    await _repoInventario.Editar(inv);
                                }
                                else
                                {
                                    await _repoInventario.Crear(new TblInventario
                                    {
                                        Clave = clave,
                                        Descripcion = desc,
                                        UnidadMedida = unidad,
                                        Existencia = (int)cantRecibida,
                                        Entrada = (int)cantRecibida,
                                        Costo = 0,
                                        Iva = 0,
                                        CostoUnitario = 0,
                                        Total = 0
                                    });
                                }
                            }
                        }

                        if (cantFaltante > 0)
                        {
                            hayFaltante = true;
                            await _repoMovimiento.Crear(new TblRequisicionDetalleMovimiento
                            {
                                IdRequisicion = childId,
                                IdRequisicionDetalle = idDetalle,
                                TipoMovimiento = "COMPRA",
                                CantidadOriginal = (int)cantSolicitada,
                                CantidadMovimiento = (int)cantFaltante,
                                FechaMovimiento = DateTime.Now,
                                IdUsuario = idUsuario,
                                Confirmado = false,
                                Observacion = $"Faltante del proveedor â€” entrega anterior: {cantRecibida}"
                            });
                            resumenFaltantes.Add($"{desc} faltante: x{cantFaltante}");
                        }
                    }

                    foreach (var b in borradores)
                        await _repoMovimiento.Eliminar(b);

                    var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == childId);
                    if (req != null)
                    {
                        var obs = new StringBuilder("Almacén registró ingreso de material del proveedor (consolidada).");
                        if (resumenEntregas.Count > 0) obs.Append($" Preparado para entrega: {string.Join(", ", resumenEntregas)}.");
                        if (resumenFaltantes.Count > 0) obs.Append($" Pendiente del proveedor: {string.Join(", ", resumenFaltantes)}.");

                        await RegistrarBitacoraAsync(childId, req.IdEstatus ?? 7, idUsuario, obs.ToString());
                    }

                    if (hayFaltante) hayFaltanteGlobal = true;
                }

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<TblConsolidada?> ObtenerConsolidadaAsync(int idConsolidada)
        {
            var query = await _repoConsolidada.Consultar(c => c.ConsolidadaId == idConsolidada);
            return await query.Include(c => c.TblPedidos).FirstOrDefaultAsync();
        }

        public async Task<TblDepartamento?> ObtenerDepartamentoRecursosMateriales()
            => await _repoDepartamento.Obtener(d => d.IdDepartamento == 19);

        public async Task<List<RequisicionMaestraDTO>> ObtenerChildRequisicionData(int idConsolidada)
        {
            var query = await _repositoryRequisicion.Consultar(r => r.ConsolidadaId == idConsolidada);
            return await query
                .Include(r => r.IdDepartamentoNavigation)
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    FechaModificacion = r.FechaModificacion,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento
                })
                .ToListAsync();
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesConDocumentos()
        {
            // Solo requisiciones con formato de entrada o salida ya subido (no pendiente).
            // Los diseños/adjuntos (TblRegistroDiseno) no son criterio de expediente.
            // Si en el futuro se requiere incluir diseños, descomentar el bloque siguiente y
            // reemplazar idsTotales por: idsFormatos.Concat(idsDisenos).Distinct()...
            //
            // var queryDisenos = await _repoRegistroDiseno.Consultar();
            // var idsDisenos = await queryDisenos.Select(d => d.IdRequisicion).Distinct().ToListAsync();

            var queryFormatos = await _repoFormato.Consultar(f => f.RutaArchivo != "PENDIENTE");
            var idsTotales = await queryFormatos
                .Select(f => f.IdRequisicion)
                .Distinct()
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToListAsync();

            if (!idsTotales.Any()) return new List<RequisicionMaestraDTO>();

            var queryReq = await _repositoryRequisicion.Consultar(r => idsTotales.Contains(r.IdRequisicion));
            return await queryReq
                .OrderByDescending(r => r.FechaModificacion)
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
                    IdEstatus = r.IdEstatus ?? 0,
                    Estatus = r.IdEstatusNavigation.NombreEstatus,
                    CantidadPartidas = r.TblRequisicionDetalles.Count
                })
                .ToListAsync();
        }

        public async Task<List<DocumentoExpedienteDTO>> ObtenerDocumentosPorRequisicion(int idRequisicion)
        {
            var resultado = new List<DocumentoExpedienteDTO>();

            var queryFormatos = await _repoFormato.Consultar(f => f.IdRequisicion == idRequisicion && f.RutaArchivo != "PENDIENTE");
            var formatos = await queryFormatos.ToListAsync();
            resultado.AddRange(formatos.Select(f => new DocumentoExpedienteDTO
            {
                Nombre = $"Formato {f.TipoFormato} #{f.NumeroFormato}",
                Tipo = f.TipoFormato,
                Ruta = f.RutaArchivo,
                FechaSubida = f.FechaFormato
            }));

            // Los diseños/adjuntos (TblRegistroDiseno) no se incluyen en el expediente de almacén.
            // Si en el futuro se necesitan mostrar, descomentar:
            //
            // var queryDisenos = await _repoRegistroDiseno.Consultar(d => d.IdRequisicion == idRequisicion);
            // var disenos = await queryDisenos.ToListAsync();
            // resultado.AddRange(disenos.Select(d => new DocumentoExpedienteDTO
            // {
            //     Nombre = d.Tipo ?? "Archivo Adjunto",
            //     Tipo = "Adjunto",
            //     Ruta = d.Ruta,
            //     FechaSubida = d.FechaSubida
            // }));

            return resultado.OrderByDescending(d => d.FechaSubida).ToList();
        }

    }
}

