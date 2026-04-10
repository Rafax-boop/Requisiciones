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

        private const int ESTATUS_EN_ALMACEN = 9;
        private const int ESTATUS_APROBADA_ALMACEN = 4;
        private const int ESTATUS_APROBADA_PARCIAL_ALMACEN = 10;
        private const int ESTATUS_RECHAZADA_ALMACEN = 5;
        private const int ESTATUS_EN_COMPRA = 11;
        private const int ESTATUS_ENTREGADO = 12;

        public AlmacenService(
            IRequisicionRepository requisicionRepository,
            IGenericRepository<TblInventario> repoInventario,
            IGenericRepository<TblEstatus> repoEstatus,
            IGenericRepository<TblBitacoraEstatus> repoBitacora,
            IUnitOfWork uow,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento)
        {
            _repositoryRequisicion = requisicionRepository;
            _repoInventario = repoInventario;
            _repoEstatus = repoEstatus;
            _repoBitacora = repoBitacora;
            _uow = uow;
            _repoMovimiento = repoMovimiento;
        }

        // Helpers privados

        private async Task<TblRequisicion> ObtenerRequisicionConDetallesAsync(int idRequisicion)
        {
            var query = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);
            var req = await query
                .Include(r => r.TblRequisicionDetalles)
                    .ThenInclude(d => d.IdArticuloNavigation)
                .FirstOrDefaultAsync();

            if (req == null)
                throw new Exception($"No se encontro la requisicion con ID {idRequisicion}.");

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
                throw new Exception($"La requisicion {req.NumRequisicion} no esta en estatus de Almacen.");
        }

        // Consultas

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

        public async Task<List<EntregaPendienteDTO>> ListarEntregasPendientes()
        {
            var movQuery = await _repoMovimiento.Consultar(
                m => m.TipoMovimiento == "ENTREGA" && m.Confirmado != true);

            var movimientos = await movQuery
                .Include(m => m.IdRequisicionNavigation)
                    .ThenInclude(r => r.IdDepartamentoNavigation)
                .Include(m => m.IdRequisicionDetalleNavigation)
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

        // El stock se descuenta aqui, cuando almacen confirma la entrega fisica.
        // ProcesarRequisicion solo registra el movimiento y valida que hay stock,
        // pero no lo toca hasta este momento.
        public async Task<bool> ConfirmarEntrega(int idRequisicion, List<int> idsMovimientos, int idUsuario)
        {
            if (idsMovimientos == null || idsMovimientos.Count == 0)
                throw new Exception("Debe seleccionar al menos un articulo para confirmar.");

            await _uow.BeginTransactionAsync();
            try
            {
                // Cargar una sola vez los detalles con navegacion al articulo
                var reqConDetalles = await ObtenerRequisicionConDetallesAsync(idRequisicion);
                var detallesPorId = reqConDetalles.TblRequisicionDetalles
                    .ToDictionary(d => d.IdRequisicionDetalle);

                foreach (var idMov in idsMovimientos)
                {
                    var mov = await _repoMovimiento.Obtener(
                        m => m.IdMovimiento == idMov && m.IdRequisicion == idRequisicion)
                        ?? throw new Exception($"No se encontro el movimiento con ID {idMov}.");

                    if (mov.Confirmado == true)
                        throw new Exception($"El movimiento {idMov} ya fue confirmado anteriormente.");

                    var det = detallesPorId.GetValueOrDefault(mov.IdRequisicionDetalle)
                        ?? throw new Exception($"No se encontro el detalle del movimiento {idMov}.");

                    var clave = (det.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (det.Descripcion ?? "").Trim();
                    var cantConfirmar = mov.CantidadMovimiento;

                    if (string.IsNullOrWhiteSpace(clave))
                        throw new Exception($"El articulo '{desc}' no tiene clave registrada.");

                    var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                        ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                    if (inv.Existencia < cantConfirmar)
                        throw new Exception(
                            $"Stock insuficiente al confirmar entrega de: {desc}. " +
                            $"Disponible: {inv.Existencia}, a entregar: {cantConfirmar}.");

                    // Descontar stock al momento de la entrega fisica
                    inv.Existencia -= cantConfirmar;
                    await _repoInventario.Editar(inv);

                    mov.Confirmado = true;
                    mov.FechaConfirmacion = DateTime.Now;
                    mov.IdUsuarioConfirmacion = idUsuario;
                    await _repoMovimiento.Editar(mov);
                }

                // Verificar si todos los movimientos de entrega quedaron confirmados
                var todosMovQuery = await _repoMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion && m.TipoMovimiento == "ENTREGA");
                var todosMovs = await todosMovQuery.ToListAsync();
                bool todoConfirmado = todosMovs.Any() && todosMovs.All(m => m.Confirmado == true);

                if (todoConfirmado)
                {
                    var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion)
                        ?? throw new Exception("No se encontro la requisicion.");

                    // Solo pasa a ENTREGADO si no tiene compras pendientes (estatus 11)
                    if (req.IdEstatus != ESTATUS_EN_COMPRA)
                    {
                        req.IdEstatus = ESTATUS_ENTREGADO;
                        req.FechaModificacion = DateTime.Now;
                        await _repositoryRequisicion.Editar(req);
                        await RegistrarBitacoraAsync(idRequisicion, ESTATUS_ENTREGADO, idUsuario,
                            "Almacen confirmo entrega fisica de todos los articulos.");
                    }
                    else
                    {
                        await RegistrarBitacoraAsync(idRequisicion, req.IdEstatus ?? 0, idUsuario,
                            "Almacen confirmo entrega fisica de articulos (pendiente proceso de compra).");
                    }
                }
                else
                {
                    var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
                    await RegistrarBitacoraAsync(idRequisicion, req?.IdEstatus ?? 0, idUsuario,
                        $"Almacen confirmo entrega parcial: {idsMovimientos.Count} articulo(s) entregado(s).");
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
                Descripcion = i.Descripcion ?? "",
                UnidadMedida = i.UnidadMedida ?? "",
                Existencia = i.Existencia,
                Minimo = 0,
                Situacion = i.Existencia == 0 ? "Sin stock" : "OK"
            }).ToList();
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

        // Operaciones con transaccion

        public async Task<bool> RegistrarIngresoInventario(IngresoInventarioDTO dto)
        {
            var descripcion = (dto.Descripcion ?? "").Trim();
            var unidadMedida = (dto.UnidadMedida ?? "").Trim();
            var motivo = (dto.Motivo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(descripcion)) throw new Exception("La descripcion es obligatoria.");
            if (string.IsNullOrWhiteSpace(unidadMedida)) throw new Exception("La unidad de medida es obligatoria.");
            if (dto.Cantidad <= 0) throw new Exception("La cantidad debe ser mayor a 0.");
            if (string.IsNullOrWhiteSpace(motivo)) throw new Exception("El motivo es obligatorio.");

            await _uow.BeginTransactionAsync();
            try
            {
                var inv = await _repoInventario.Obtener(i => i.Descripcion == descripcion && i.UnidadMedida == unidadMedida);

                if (inv == null)
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

        public async Task<bool> AprobarRequisicionCompleta(int idRequisicion, int idUsuario)
        {
            await _uow.BeginTransactionAsync();
            try
            {
                var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);
                ValidarEstatusAlmacen(req);

                var detalles = req.TblRequisicionDetalles.ToList();
                if (detalles.Count == 0)
                    throw new Exception("La requisicion no tiene partidas.");

                var resumenEgresos = new List<string>();

                foreach (var d in detalles)
                {
                    var cant = (int)Math.Ceiling(d.Cantidad ?? 0m);
                    if (cant <= 0) continue;

                    var clave = (d.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (d.Descripcion ?? "").Trim();
                    var unidad = (d.UnidadMedida ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))
                        throw new Exception("Hay partidas sin descripcion o unidad de medida.");

                    var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                        ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                    if (inv.Existencia < cant)
                        throw new Exception($"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, requerido: {cant}.");

                    inv.Existencia -= cant;
                    await _repoInventario.Editar(inv);
                    resumenEgresos.Add($"{desc} x{cant}");
                }

                req.IdEstatus = ESTATUS_APROBADA_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                await RegistrarBitacoraAsync(req.IdRequisicion, ESTATUS_APROBADA_ALMACEN, idUsuario,
                    $"Almacen autorizo completa. Egreso: {string.Join(", ", resumenEgresos)}.");

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> AprobarRequisicionParcial(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> partidas)
        {
            var lista = (partidas ?? Enumerable.Empty<(int, int)>()).ToList();
            if (lista.Count == 0)
                throw new Exception("No se recibieron partidas para aprobar.");
            if (lista.Any(x => x.cantidadAprobada < 0))
                throw new Exception("Las cantidades aprobadas no pueden ser negativas.");
            if (!lista.Any(x => x.cantidadAprobada > 0))
                throw new Exception("Debe aprobar al menos una partida con cantidad mayor a 0.");

            await _uow.BeginTransactionAsync();
            try
            {
                var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);
                ValidarEstatusAlmacen(req);

                var detallesById = req.TblRequisicionDetalles.ToDictionary(d => d.IdRequisicionDetalle);
                var resumenEgresos = new List<string>();
                var faltantes = new List<string>();

                foreach (var (idDetalle, cantAprobada) in lista)
                {
                    if (cantAprobada <= 0) continue;

                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        throw new Exception("Una de las partidas no pertenece a la requisicion.");

                    var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);
                    if (cantAprobada > cantSolicitada)
                        throw new Exception($"La cantidad aprobada ({cantAprobada}) no puede exceder la solicitada ({cantSolicitada}).");

                    var clave = (d.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (d.Descripcion ?? "").Trim();
                    var unidad = (d.UnidadMedida ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))
                        throw new Exception("Hay partidas sin descripcion o unidad de medida.");

                    var inv = await _repoInventario.Obtener(i => i.Clave == clave)
                        ?? throw new Exception($"No existe el material en inventario con clave: {clave} ({desc}).");

                    if (inv.Existencia < cantAprobada)
                        throw new Exception($"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, aprobado: {cantAprobada}.");

                    inv.Existencia -= cantAprobada;
                    await _repoInventario.Editar(inv);
                    resumenEgresos.Add($"{desc} x{cantAprobada}");

                    if (cantAprobada < cantSolicitada)
                        faltantes.Add($"{desc}: surtido {cantAprobada}/{cantSolicitada}");
                }

                foreach (var det in detallesById.Values)
                {
                    if (!lista.Any(x => x.idRequisicionDetalle == det.IdRequisicionDetalle && x.cantidadAprobada > 0))
                    {
                        var cantSol = (int)Math.Ceiling(det.Cantidad ?? 0m);
                        if (cantSol > 0)
                            faltantes.Add($"{(det.Descripcion ?? "").Trim()}: no surtido (solicitado {cantSol})");
                    }
                }

                req.IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                var obs = new StringBuilder("Almacen autorizo parcial. Egreso: ");
                obs.Append(string.Join(", ", resumenEgresos));
                if (faltantes.Count > 0)
                    obs.Append($". FALTANTES: {string.Join("; ", faltantes)}. Urgente resurtir stock.");

                await RegistrarBitacoraAsync(req.IdRequisicion, ESTATUS_APROBADA_PARCIAL_ALMACEN, idUsuario, obs.ToString());

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
                    ?? throw new Exception($"No se encontro la requisicion con ID {idRequisicion}.");

                ValidarEstatusAlmacen(req);

                req.IdEstatus = ESTATUS_RECHAZADA_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                await RegistrarBitacoraAsync(req.IdRequisicion, ESTATUS_RECHAZADA_ALMACEN, idUsuario,
                    $"Almacen rechazo requisicion. Motivo: {motivo}");

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
            IEnumerable<(int idRequisicionDetalle, int cantidadComprar)> compras)
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

                // Entregas: solo validar existencia y registrar movimiento.
                // El stock NO se descuenta aqui; se descuenta en ConfirmarEntrega
                // cuando almacen entrega fisicamente el material.
                foreach (var (idDetalle, cantAprobada) in listaEntregas)
                {
                    if (cantAprobada <= 0) continue;

                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        throw new Exception("Una de las partidas de entrega no pertenece a la requisicion.");

                    var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);
                    if (cantAprobada > cantSolicitada)
                        throw new Exception($"La cantidad aprobada ({cantAprobada}) no puede exceder la solicitada ({cantSolicitada}).");

                    var clave = (d.IdArticuloNavigation?.Clave ?? "").Trim();
                    var desc = (d.Descripcion ?? "").Trim();

                    if (string.IsNullOrWhiteSpace(clave))
                        throw new Exception($"El articulo '{desc}' no tiene clave registrada.");

                    // Solo validar que existe y tiene stock — el descuento es al confirmar entrega
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

                    resumenEntregas.Add($"{desc} x{cantAprobada}");
                }

                // Compras: solo registrar movimiento, sin tocar stock
                foreach (var (idDetalle, cantComprar) in listaCompras)
                {
                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        throw new Exception("Una de las partidas de compra no pertenece a la requisicion.");

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

                // Con compras -> estatus 11 (regresa a materiales)
                // Solo entregas -> queda en 9 para que aparezca en tab "A Entregar"
                //                  y al confirmar pasa a 12 (ENTREGADO)
                int estatusFinal = listaCompras.Count > 0 ? ESTATUS_EN_COMPRA : ESTATUS_EN_ALMACEN;

                req.IdEstatus = estatusFinal;
                req.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(req);

                var obs = new StringBuilder("Almacen proceso requisicion.");
                if (resumenEntregas.Count > 0) obs.Append($" Preparados para entrega: {string.Join(", ", resumenEntregas)}.");
                if (resumenCompras.Count > 0) obs.Append($" Enviados a compra: {string.Join(", ", resumenCompras)}.");

                await RegistrarBitacoraAsync(req.IdRequisicion, estatusFinal, idUsuario, obs.ToString());

                await _uow.CommitAsync();
                return true;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}