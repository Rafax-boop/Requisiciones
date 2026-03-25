using Inventario.BLL.DTO;

using Inventario.BLL.Interfaces;

using Inventario.DAL.DBCONTEXT;

using Inventario.DAL.Interfaces;

using Inventario.Entity;

using Microsoft.EntityFrameworkCore;

using System;

using System.Collections.Generic;

using System.Linq;

using System.Text;

using System.Threading.Tasks;



namespace Inventario.BLL.Implementacion

{

    public class AlmacenService : IAlmacenService

    {

        private readonly IRequisicionRepository _repositoryRequisicion;

        private readonly IGenericRepository<TblInventario> _repoInventario;

        private readonly IGenericRepository<TblEstatus> _repoEstatus;

        private readonly IGenericRepository<TblBitacoraEstatus> _repoBitacora;

        private readonly IGenericRepository<TblMovimientoInventario> _repoMovimiento;

        private readonly DbSigereContext _dbContext;



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

            IGenericRepository<TblMovimientoInventario> repoMovimiento,

            DbSigereContext dbContext)

        {

            _repositoryRequisicion = requisicionRepository;

            _repoEstatus = repoEstatus;

            _repoInventario = repoInventario;

            _repoBitacora = repoBitacora;

            _repoMovimiento = repoMovimiento;

            _dbContext = dbContext;

        }



        private async Task<TblRequisicion?> ObtenerRequisicionConDetallesAsync(int idRequisicion)

        {

            var query = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);

            return await query.Include(r => r.TblRequisicionDetalles).FirstOrDefaultAsync();

        }



        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen()

        {

            IQueryable<TblRequisicion> query = await _repositoryRequisicion.Consultar(r => r.IdEstatus == ESTATUS_EN_ALMACEN);



            var resultado = await query

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



            return resultado;

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



        //    public async Task<(bool ok, string? mensaje, string? error)> RegistrarIngresoInventario(

        //        string? clave,

        //        string descripcion,

        //        string unidadMedida,

        //        int cantidad,

        //        int idUsuario,

        //        string motivo)

        //    {

        //        descripcion = (descripcion ?? "").Trim();

        //        unidadMedida = (unidadMedida ?? "").Trim();

        //        motivo = (motivo ?? "").Trim();



        //        if (string.IsNullOrWhiteSpace(descripcion)) return (false, null, "La descripción es obligatoria.");

        //        if (string.IsNullOrWhiteSpace(unidadMedida)) return (false, null, "La unidad de medida es obligatoria.");

        //        if (cantidad <= 0) return (false, null, "La cantidad debe ser mayor a 0.");

        //        if (string.IsNullOrWhiteSpace(motivo)) return (false, null, "El motivo es obligatorio.");



        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var inv = await _repoInventario.Obtener(i => i.Descripcion == descripcion && i.UnidadMedida == unidadMedida);



        //            bool esNuevo = inv == null;



        //            if (inv == null)

        //            {

        //                if (string.IsNullOrWhiteSpace(clave)) return (false, null, "La clave es obligatoria para dar de alta un material nuevo.");



        //                inv = new TblInventario

        //                {

        //                    Clave = clave.Trim(),

        //                    Descripcion = descripcion,

        //                    UnidadMedida = unidadMedida,

        //                    Entrada = 0,

        //                    Existencia = 0,

        //                    Costo = 0,

        //                    Iva = 0,

        //                    CostoUnitario = 0,

        //                    Total = 0

        //                };

        //                inv = await _repoInventario.Crear(inv);

        //            }



        //            inv.Existencia += cantidad;

        //            await _repoInventario.Editar(inv);



        //            await _repoMovimiento.Crear(new TblMovimientoInventario

        //            {

        //                IdInventario = inv.Id,

        //                TipoMovimiento = "I",

        //                Cantidad = cantidad,

        //                Fecha = DateTime.Now,

        //                Motivo = motivo,

        //                IdUsuario = idUsuario,

        //                Anulado = false

        //            });



        //            await tx.CommitAsync();



        //            var msg = esNuevo

        //                ? $"Material nuevo registrado: {descripcion} ({unidadMedida}). Stock: {inv.Existencia}."

        //                : $"Ingreso registrado para {descripcion} ({unidadMedida}). Nuevo stock: {inv.Existencia}.";



        //            return (true, msg, null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo registrar el ingreso. " + ex.Message);

        //        }

        //    }



        //    public async Task<(bool ok, string? mensaje, string? error)> AprobarRequisicionCompleta(int idRequisicion, int idUsuario)

        //    {

        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);



        //            if (req == null) return (false, null, "No se encontró la requisición.");

        //            if (req.IdEstatus != ESTATUS_EN_ALMACEN)

        //                return (false, null, "La requisición no está en estatus de Almacén. No se puede aprobar.");



        //            var detalles = req.TblRequisicionDetalles.ToList();

        //            if (detalles.Count == 0) return (false, null, "La requisición no tiene partidas.");



        //            var resumenEgresos = new List<string>();



        //            foreach (var d in detalles)

        //            {

        //                var cantDecimal = d.Cantidad ?? 0m;

        //                if (cantDecimal <= 0) continue;

        //                var cant = (int)Math.Ceiling(cantDecimal);



        //                var desc = (d.Descripcion ?? "").Trim();

        //                var unidad = (d.UnidadMedida ?? "").Trim();

        //                if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))

        //                    return (false, null, "Hay partidas sin descripción o unidad de medida.");



        //                var inv = await _repoInventario.Obtener(i => i.Descripcion == desc && i.UnidadMedida == unidad);

        //                if (inv == null) return (false, null, $"No existe el material en inventario: {desc} ({unidad}).");

        //                if (inv.Existencia < cant)

        //                    return (false, null, $"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, requerido: {cant}. Considere aprobar parcial.");



        //                inv.Existencia -= cant;

        //                await _repoInventario.Editar(inv);



        //                await _repoMovimiento.Crear(new TblMovimientoInventario

        //                {

        //                    IdInventario = inv.Id,

        //                    TipoMovimiento = "E",

        //                    Cantidad = cant,

        //                    Fecha = DateTime.Now,

        //                    Motivo = $"Egreso por autorización completa requisición {req.NumRequisicion ?? req.IdRequisicion.ToString()}",

        //                    IdRequisicion = req.IdRequisicion,

        //                    IdRequisicionDetalle = d.IdRequisicionDetalle,

        //                    IdUsuario = idUsuario,

        //                    Anulado = false

        //                });



        //                resumenEgresos.Add($"{desc} x{cant}");

        //            }



        //            req.IdEstatus = ESTATUS_APROBADA_ALMACEN;

        //            req.FechaModificacion = DateTime.Now;

        //            await _repositoryRequisicion.Editar(req);



        //            var observacion = $"Almacén autorizó completa. Egreso aplicado: {string.Join(", ", resumenEgresos)}.";



        //            await _repoBitacora.Crear(new TblBitacoraEstatus

        //            {

        //                IdRequisicion = req.IdRequisicion,

        //                IdEstatus = ESTATUS_APROBADA_ALMACEN,

        //                FechaEstatus = DateTime.Now,

        //                Observacion = observacion.Length > 1000 ? observacion.Substring(0, 1000) : observacion,

        //                IdUsuario = idUsuario

        //            });



        //            await tx.CommitAsync();

        //            return (true, $"Requisición {req.NumRequisicion} autorizada completa. Se descontó el inventario.", null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo aprobar la requisición. " + ex.Message);

        //        }

        //    }



        //    public async Task<(bool ok, string? mensaje, string? error)> AprobarRequisicionParcial(

        //        int idRequisicion,

        //        int idUsuario,

        //        IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> partidas)

        //    {

        //        var lista = (partidas ?? Enumerable.Empty<(int, int)>()).ToList();

        //        if (lista.Count == 0) return (false, null, "No se recibieron partidas para aprobar.");

        //        if (lista.Any(x => x.cantidadAprobada < 0)) return (false, null, "Las cantidades aprobadas no pueden ser negativas.");



        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);



        //            if (req == null) return (false, null, "No se encontró la requisición.");

        //            if (req.IdEstatus != ESTATUS_EN_ALMACEN)

        //                return (false, null, "La requisición no está en estatus de Almacén. No se puede aprobar.");



        //            var detallesById = req.TblRequisicionDetalles.ToDictionary(d => d.IdRequisicionDetalle);



        //            bool haySurtido = lista.Any(x => x.cantidadAprobada > 0);

        //            if (!haySurtido) return (false, null, "Debe aprobar al menos una partida con cantidad mayor a 0.");



        //            var resumenEgresos = new List<string>();

        //            var faltantes = new List<string>();



        //            foreach (var (idDetalle, cantAprobada) in lista)

        //            {

        //                if (cantAprobada <= 0) continue;

        //                if (!detallesById.TryGetValue(idDetalle, out var d))

        //                    return (false, null, "Una de las partidas no pertenece a la requisición.");



        //                var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);

        //                if (cantAprobada > cantSolicitada)

        //                    return (false, null, $"La cantidad aprobada ({cantAprobada}) no puede exceder la solicitada ({cantSolicitada}).");



        //                var desc = (d.Descripcion ?? "").Trim();

        //                var unidad = (d.UnidadMedida ?? "").Trim();

        //                if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))

        //                    return (false, null, "Hay partidas sin descripción o unidad de medida.");



        //                var inv = await _repoInventario.Obtener(i => i.Descripcion == desc && i.UnidadMedida == unidad);

        //                if (inv == null) return (false, null, $"No existe el material en inventario: {desc} ({unidad}).");

        //                if (inv.Existencia < cantAprobada)

        //                    return (false, null, $"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, aprobado: {cantAprobada}.");



        //                inv.Existencia -= cantAprobada;

        //                await _repoInventario.Editar(inv);



        //                await _repoMovimiento.Crear(new TblMovimientoInventario

        //                {

        //                    IdInventario = inv.Id,

        //                    TipoMovimiento = "E",

        //                    Cantidad = cantAprobada,

        //                    Fecha = DateTime.Now,

        //                    Motivo = $"Egreso por autorización parcial requisición {req.NumRequisicion ?? req.IdRequisicion.ToString()}",

        //                    IdRequisicion = req.IdRequisicion,

        //                    IdRequisicionDetalle = d.IdRequisicionDetalle,

        //                    IdUsuario = idUsuario,

        //                    Anulado = false

        //                });



        //                resumenEgresos.Add($"{desc} x{cantAprobada}");



        //                if (cantAprobada < cantSolicitada)

        //                    faltantes.Add($"{desc}: surtido {cantAprobada}/{cantSolicitada}");

        //            }



        //            foreach (var det in detallesById.Values)

        //            {

        //                if (!lista.Any(x => x.idRequisicionDetalle == det.IdRequisicionDetalle && x.cantidadAprobada > 0))

        //                {

        //                    var cantSol = (int)Math.Ceiling(det.Cantidad ?? 0m);

        //                    if (cantSol > 0)

        //                        faltantes.Add($"{(det.Descripcion ?? "").Trim()}: no surtido (solicitado {cantSol})");

        //                }

        //            }



        //            req.IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN;

        //            req.FechaModificacion = DateTime.Now;

        //            await _repositoryRequisicion.Editar(req);



        //            var obsBuilder = new StringBuilder("Almacén autorizó parcial. Egreso: ");

        //            obsBuilder.Append(string.Join(", ", resumenEgresos));

        //            if (faltantes.Count > 0)

        //            {

        //                obsBuilder.Append(". FALTANTES: ");

        //                obsBuilder.Append(string.Join("; ", faltantes));

        //                obsBuilder.Append(". Urgente resurtir stock.");

        //            }

        //            var observacion = obsBuilder.ToString();



        //            await _repoBitacora.Crear(new TblBitacoraEstatus

        //            {

        //                IdRequisicion = req.IdRequisicion,

        //                IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN,

        //                FechaEstatus = DateTime.Now,

        //                Observacion = observacion.Length > 1000 ? observacion.Substring(0, 1000) : observacion,

        //                IdUsuario = idUsuario

        //            });



        //            await tx.CommitAsync();



        //            var msgFaltante = faltantes.Count > 0

        //                ? $" Faltantes: {string.Join("; ", faltantes)}. Urgente resurtir stock."

        //                : "";



        //            return (true, $"Requisición {req.NumRequisicion} autorizada parcial. Se descontó el inventario.{msgFaltante}", null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo aprobar parcialmente. " + ex.Message);

        //        }

        //    }



        //    public async Task<(bool ok, string? mensaje, string? error)> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo)

        //    {

        //        motivo = (motivo ?? "").Trim();

        //        if (string.IsNullOrWhiteSpace(motivo)) return (false, null, "El motivo de rechazo es obligatorio.");



        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var req = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);

        //            if (req == null) return (false, null, "No se encontró la requisición.");

        //            if (req.IdEstatus != ESTATUS_EN_ALMACEN)

        //                return (false, null, "La requisición no está en estatus de Almacén. No se puede rechazar.");



        //            req.IdEstatus = ESTATUS_RECHAZADA_ALMACEN;

        //            req.FechaModificacion = DateTime.Now;

        //            await _repositoryRequisicion.Editar(req);



        //            var observacion = $"Almacén rechazó requisición. Motivo: {motivo}";



        //            await _repoBitacora.Crear(new TblBitacoraEstatus

        //            {

        //                IdRequisicion = req.IdRequisicion,

        //                IdEstatus = ESTATUS_RECHAZADA_ALMACEN,

        //                FechaEstatus = DateTime.Now,

        //                Observacion = observacion.Length > 1000 ? observacion.Substring(0, 1000) : observacion,

        //                IdUsuario = idUsuario

        //            });



        //            await tx.CommitAsync();

        //            return (true, $"Requisición {req.NumRequisicion} rechazada.", null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo rechazar. " + ex.Message);

        //        }

        //    }



        //    public async Task<(bool ok, string? mensaje, string? error)> AnularMovimientoInventario(int idMovimiento, int idUsuario, string motivoAnulacion)

        //    {

        //        motivoAnulacion = (motivoAnulacion ?? "").Trim();

        //        if (string.IsNullOrWhiteSpace(motivoAnulacion)) return (false, null, "El motivo de anulación es obligatorio.");



        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var mov = await _repoMovimiento.Obtener(m => m.Id == idMovimiento);

        //            if (mov == null) return (false, null, "No se encontró el movimiento.");

        //            if (mov.Anulado) return (false, null, "El movimiento ya está anulado.");



        //            var inv = await _repoInventario.Obtener(i => i.Id == mov.IdInventario);

        //            if (inv == null) return (false, null, "No se encontró el inventario asociado.");



        //            if (mov.TipoMovimiento == "E")

        //            {

        //                inv.Existencia += mov.Cantidad;

        //            }

        //            else if (mov.TipoMovimiento == "I")

        //            {

        //                if (inv.Existencia < mov.Cantidad) return (false, null, "No se puede anular: el stock quedaría negativo.");

        //                inv.Existencia -= mov.Cantidad;

        //            }



        //            mov.Anulado = true;

        //            mov.MotivoAnulacion = motivoAnulacion;

        //            mov.FechaAnulacion = DateTime.Now;

        //            mov.IdUsuarioAnula = idUsuario;



        //            await _repoInventario.Editar(inv);

        //            await _repoMovimiento.Editar(mov);



        //            await tx.CommitAsync();

        //            return (true, "Movimiento anulado correctamente. Stock actualizado.", null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo anular el movimiento. " + ex.Message);

        //        }

        //    }



        //    public async Task<(bool ok, string? mensaje, string? error)> ProcesarRequisicion(

        //        int idRequisicion,

        //        int idUsuario,

        //        IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> entregas,

        //        IEnumerable<(int idRequisicionDetalle, int cantidadComprar)> compras)

        //    {

        //        var listaEntregas = (entregas ?? Enumerable.Empty<(int, int)>()).ToList();

        //        var listaCompras = (compras ?? Enumerable.Empty<(int, int)>()).ToList();



        //        if (listaEntregas.Count == 0 && listaCompras.Count == 0)

        //            return (false, null, "Debe indicar al menos una entrega o una compra.");

        //        if (listaEntregas.Any(x => x.cantidadAprobada < 0))

        //            return (false, null, "Las cantidades de entrega no pueden ser negativas.");

        //        if (listaCompras.Any(x => x.cantidadComprar <= 0))

        //            return (false, null, "Las cantidades de compra deben ser mayores a 0.");



        //        var comprasSet = new HashSet<int>(listaCompras.Select(c => c.idRequisicionDetalle));



        //        await using var tx = await _dbContext.Database.BeginTransactionAsync();

        //        try

        //        {

        //            var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);



        //            if (req == null) return (false, null, "No se encontró la requisición.");

        //            if (req.IdEstatus != ESTATUS_EN_ALMACEN)

        //                return (false, null, "La requisición no está en estatus de Almacén. No se puede procesar.");



        //            var detallesById = req.TblRequisicionDetalles.ToDictionary(d => d.IdRequisicionDetalle);



        //            var resumenEntregas = new List<string>();

        //            var resumenCompras = new List<string>();



        //            foreach (var (idDetalle, cantAprobada) in listaEntregas)

        //            {

        //                if (cantAprobada <= 0) continue;

        //                if (!detallesById.TryGetValue(idDetalle, out var d))

        //                    return (false, null, "Una de las partidas de entrega no pertenece a la requisición.");



        //                var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);

        //                if (cantAprobada > cantSolicitada)

        //                    return (false, null, $"La cantidad aprobada ({cantAprobada}) no puede exceder la solicitada ({cantSolicitada}).");



        //                var desc = (d.Descripcion ?? "").Trim();

        //                var unidad = (d.UnidadMedida ?? "").Trim();

        //                if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))

        //                    return (false, null, "Hay partidas sin descripción o unidad de medida.");



        //                var inv = await _repoInventario.Obtener(i => i.Descripcion == desc && i.UnidadMedida == unidad);

        //                if (inv == null) return (false, null, $"No existe el material en inventario: {desc} ({unidad}).");

        //                if (inv.Existencia < cantAprobada)

        //                    return (false, null, $"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, aprobado: {cantAprobada}.");



        //                inv.Existencia -= cantAprobada;

        //                await _repoInventario.Editar(inv);



        //                await _repoMovimiento.Crear(new TblMovimientoInventario

        //                {

        //                    IdInventario = inv.Id,

        //                    TipoMovimiento = "E",

        //                    Cantidad = cantAprobada,

        //                    Fecha = DateTime.Now,

        //                    Motivo = $"Egreso por procesamiento requisición {req.NumRequisicion ?? req.IdRequisicion.ToString()}",

        //                    IdRequisicion = req.IdRequisicion,

        //                    IdRequisicionDetalle = d.IdRequisicionDetalle,

        //                    IdUsuario = idUsuario,

        //                    Anulado = false

        //                });



        //                resumenEntregas.Add($"{desc} x{cantAprobada}");



        //                if (comprasSet.Contains(idDetalle))

        //                    d.IdEstatus = ESTATUS_EN_COMPRA;

        //                else

        //                    d.IdEstatus = ESTATUS_ENTREGADO;

        //            }



        //            foreach (var (idDetalle, cantComprar) in listaCompras)

        //            {

        //                if (!detallesById.TryGetValue(idDetalle, out var d))

        //                    return (false, null, "Una de las partidas de compra no pertenece a la requisición.");



        //                d.IdEstatus = ESTATUS_EN_COMPRA;

        //                resumenCompras.Add($"{(d.Descripcion ?? "").Trim()} x{cantComprar}");

        //            }



        //            req.IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN;

        //            req.FechaModificacion = DateTime.Now;

        //            await _repositoryRequisicion.Editar(req);



        //            var obsBuilder = new StringBuilder("Almacén procesó requisición.");

        //            if (resumenEntregas.Count > 0)

        //            {

        //                obsBuilder.Append(" Entregados: ");

        //                obsBuilder.Append(string.Join(", ", resumenEntregas));

        //                obsBuilder.Append('.');

        //            }

        //            if (resumenCompras.Count > 0)

        //            {

        //                obsBuilder.Append(" Enviados a compra: ");

        //                obsBuilder.Append(string.Join(", ", resumenCompras));

        //                obsBuilder.Append('.');

        //            }

        //            var observacion = obsBuilder.ToString();



        //            await _repoBitacora.Crear(new TblBitacoraEstatus

        //            {

        //                IdRequisicion = req.IdRequisicion,

        //                IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN,

        //                FechaEstatus = DateTime.Now,

        //                Observacion = observacion.Length > 1000 ? observacion.Substring(0, 1000) : observacion,

        //                IdUsuario = idUsuario

        //            });



        //            await tx.CommitAsync();



        //            var msg = $"Requisición {req.NumRequisicion} procesada.";

        //            if (resumenEntregas.Count > 0) msg += $" Entregados: {resumenEntregas.Count} material(es).";

        //            if (resumenCompras.Count > 0) msg += $" Enviados a compra: {resumenCompras.Count} material(es).";



        //            return (true, msg, null);

        //        }

        //        catch (Exception ex)

        //        {

        //            await tx.RollbackAsync();

        //            return (false, null, "No se pudo procesar la requisición. " + ex.Message);

        //        }

        //    }



        //    public async Task<List<StockPartidasDTO>> ConsultarStockParaRequisicion(int idRequisicion)

        //    {

        //        var req = await ObtenerRequisicionConDetallesAsync(idRequisicion);



        //        if (req == null) return new List<StockPartidasDTO>();



        //        var resultado = new List<StockPartidasDTO>();



        //        foreach (var d in req.TblRequisicionDetalles)

        //        {

        //            var desc = (d.Descripcion ?? "").Trim();

        //            var unidad = (d.UnidadMedida ?? "").Trim();

        //            var cantSolicitada = (int)Math.Ceiling(d.Cantidad ?? 0m);



        //            var inv = await _repoInventario.Obtener(i => i.Descripcion == desc && i.UnidadMedida == unidad);



        //            resultado.Add(new StockPartidasDTO

        //            {

        //                IdRequisicionDetalle = d.IdRequisicionDetalle,

        //                Descripcion = desc,

        //                UnidadMedida = unidad,

        //                CantidadSolicitada = cantSolicitada,

        //                StockDisponible = inv?.Existencia ?? 0,

        //                ExisteEnInventario = inv != null

        //            });

        //        }



        //        return resultado;

        //    }

        //}

    }
}

