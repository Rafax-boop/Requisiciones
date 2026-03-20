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
        private readonly DbSigereContext _dbContext;

        // Mapeo con tu TblEstatus (según tu tabla SQL):
        // IdEstatus=4  -> REQUISICION AUTORIZADA (aprobada completa)
        // IdEstatus=5  -> REQUISICION RECHAZADA (rechazada)
        // IdEstatus=10 -> REQUISICION AUTORIZADA PARCIAL (aprobada parcial)
        private const int ESTATUS_APROBADA_ALMACEN = 4;
        private const int ESTATUS_APROBADA_PARCIAL_ALMACEN = 10;
        private const int ESTATUS_RECHAZADA_ALMACEN = 5;

        public AlmacenService(
            IRequisicionRepository requisicionRepository,
            IGenericRepository<TblInventario> repoInventario,
            IGenericRepository<TblEstatus> repoEstatus,
            IGenericRepository<TblBitacoraEstatus> repoBitacora,
            DbSigereContext dbContext)
        {
            _repositoryRequisicion = requisicionRepository;
            _repoEstatus = repoEstatus;
            _repoInventario = repoInventario;
            _repoBitacora = repoBitacora;
            _dbContext = dbContext;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen()
        {
            IQueryable<TblRequisicion> query = await _repositoryRequisicion.Consultar(r => r.IdEstatus == 9);

            var resultado = await query
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
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

        public async Task<(bool ok, string? error)> RegistrarIngresoInventario(
            string? clave,
            string descripcion,
            string unidadMedida,
            int cantidad,
            int idUsuario,
            string motivo)
        {
            descripcion = (descripcion ?? "").Trim();
            unidadMedida = (unidadMedida ?? "").Trim();
            motivo = (motivo ?? "").Trim();

            if (string.IsNullOrWhiteSpace(descripcion)) return (false, "La descripción es obligatoria.");
            if (string.IsNullOrWhiteSpace(unidadMedida)) return (false, "La unidad de medida es obligatoria.");
            if (cantidad <= 0) return (false, "La cantidad debe ser mayor a 0.");
            if (string.IsNullOrWhiteSpace(motivo)) return (false, "El motivo es obligatorio.");

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var inv = await _dbContext.TblInventarios
                    .FirstOrDefaultAsync(i => i.Descripcion == descripcion && i.UnidadMedida == unidadMedida);

                if (inv == null)
                {
                    if (string.IsNullOrWhiteSpace(clave)) return (false, "La clave es obligatoria para dar de alta un material nuevo.");

                    inv = new TblInventario
                    {
                        Clave = clave.Trim(),
                        Descripcion = descripcion,
                        UnidadMedida = unidadMedida,
                        Entrada = 0,
                        Existencia = 0,
                        Costo = 0,
                        Iva = 0,
                        CostoUnitario = 0,
                        Total = 0
                    };
                    _dbContext.TblInventarios.Add(inv);
                    await _dbContext.SaveChangesAsync();
                }

                inv.Existencia += cantidad;
                _dbContext.TblInventarios.Update(inv);

                _dbContext.Set<TblMovimientoInventario>().Add(new TblMovimientoInventario
                {
                    IdInventario = inv.Id,
                    TipoMovimiento = "I",
                    Cantidad = cantidad,
                    Fecha = DateTime.Now,
                    Motivo = motivo,
                    IdUsuario = idUsuario,
                    Anulado = false
                });

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "No se pudo registrar el ingreso. " + ex.Message);
            }
        }

        public async Task<(bool ok, string? error)> AprobarRequisicionCompleta(int idRequisicion, int idUsuario)
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var req = await _dbContext.TblRequisicions
                    .Include(r => r.TblRequisicionDetalles)
                    .FirstOrDefaultAsync(r => r.IdRequisicion == idRequisicion);

                if (req == null) return (false, "No se encontró la requisición.");

                var detalles = req.TblRequisicionDetalles.ToList();
                if (detalles.Count == 0) return (false, "La requisición no tiene partidas.");

                foreach (var d in detalles)
                {
                    var cantDecimal = d.Cantidad ?? 0m;
                    if (cantDecimal <= 0) continue;
                    if (cantDecimal % 1m != 0m) return (false, "La cantidad debe ser un entero (piezas).");
                    var cant = (int)cantDecimal;

                    var desc = (d.Descripcion ?? "").Trim();
                    var unidad = (d.UnidadMedida ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))
                        return (false, "Hay partidas sin descripción o unidad de medida.");

                    var inv = await _dbContext.TblInventarios
                        .FirstOrDefaultAsync(i => i.Descripcion == desc && i.UnidadMedida == unidad);
                    if (inv == null) return (false, $"No existe el material en inventario: {desc} ({unidad}).");
                    if (inv.Existencia < cant) return (false, $"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, requerido: {cant}.");

                    inv.Existencia -= cant;
                    _dbContext.TblInventarios.Update(inv);

                    _dbContext.Set<TblMovimientoInventario>().Add(new TblMovimientoInventario
                    {
                        IdInventario = inv.Id,
                        TipoMovimiento = "E",
                        Cantidad = cant,
                        Fecha = DateTime.Now,
                        Motivo = $"Egreso por aprobación requisición {req.NumRequisicion ?? req.IdRequisicion.ToString()}",
                        IdRequisicion = req.IdRequisicion,
                        IdRequisicionDetalle = d.IdRequisicionDetalle,
                        IdUsuario = idUsuario,
                        Anulado = false
                    });
                }

                req.IdEstatus = ESTATUS_APROBADA_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                _dbContext.TblRequisicions.Update(req);

                await _repoBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = req.IdRequisicion,
                    IdEstatus = ESTATUS_APROBADA_ALMACEN,
                    FechaEstatus = DateTime.Now,
                    Observacion = "Aprobada (Almacén)",
                    IdUsuario = idUsuario
                });

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "No se pudo aprobar la requisición. " + ex.Message);
            }
        }

        public async Task<(bool ok, string? error)> AprobarRequisicionParcial(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> partidas)
        {
            var lista = (partidas ?? Enumerable.Empty<(int, int)>()).ToList();
            if (lista.Count == 0) return (false, "No se recibieron partidas para aprobar.");
            if (lista.Any(x => x.cantidadAprobada < 0)) return (false, "Las cantidades aprobadas no pueden ser negativas.");

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var req = await _dbContext.TblRequisicions
                    .Include(r => r.TblRequisicionDetalles)
                    .FirstOrDefaultAsync(r => r.IdRequisicion == idRequisicion);

                if (req == null) return (false, "No se encontró la requisición.");

                var detallesById = req.TblRequisicionDetalles.ToDictionary(d => d.IdRequisicionDetalle);

                foreach (var (idDetalle, cantAprobada) in lista)
                {
                    if (cantAprobada <= 0) continue;
                    if (!detallesById.TryGetValue(idDetalle, out var d))
                        return (false, "Una de las partidas no pertenece a la requisición.");

                    var cantSolicitada = d.Cantidad ?? 0m;
                    if (cantSolicitada % 1m != 0m) return (false, "La cantidad solicitada debe ser un entero (piezas).");
                    if (cantAprobada > (int)cantSolicitada) return (false, "La cantidad aprobada no puede exceder la solicitada.");

                    var desc = (d.Descripcion ?? "").Trim();
                    var unidad = (d.UnidadMedida ?? "").Trim();
                    if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(unidad))
                        return (false, "Hay partidas sin descripción o unidad de medida.");

                    var inv = await _dbContext.TblInventarios
                        .FirstOrDefaultAsync(i => i.Descripcion == desc && i.UnidadMedida == unidad);
                    if (inv == null) return (false, $"No existe el material en inventario: {desc} ({unidad}).");
                    if (inv.Existencia < cantAprobada) return (false, $"Stock insuficiente para: {desc} ({unidad}). Disponible: {inv.Existencia}, requerido: {cantAprobada}.");

                    inv.Existencia -= cantAprobada;
                    _dbContext.TblInventarios.Update(inv);

                    _dbContext.Set<TblMovimientoInventario>().Add(new TblMovimientoInventario
                    {
                        IdInventario = inv.Id,
                        TipoMovimiento = "E",
                        Cantidad = cantAprobada,
                        Fecha = DateTime.Now,
                        Motivo = $"Egreso por aprobación parcial requisición {req.NumRequisicion ?? req.IdRequisicion.ToString()}",
                        IdRequisicion = req.IdRequisicion,
                        IdRequisicionDetalle = d.IdRequisicionDetalle,
                        IdUsuario = idUsuario,
                        Anulado = false
                    });
                }

                req.IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                _dbContext.TblRequisicions.Update(req);

                await _repoBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = req.IdRequisicion,
                    IdEstatus = ESTATUS_APROBADA_PARCIAL_ALMACEN,
                    FechaEstatus = DateTime.Now,
                    Observacion = "Aprobada Parcial (Almacén)",
                    IdUsuario = idUsuario
                });

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "No se pudo aprobar parcialmente. " + ex.Message);
            }
        }

        public async Task<(bool ok, string? error)> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo)
        {
            motivo = (motivo ?? "").Trim();
            if (string.IsNullOrWhiteSpace(motivo)) return (false, "El motivo es obligatorio.");

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var req = await _dbContext.TblRequisicions.FirstOrDefaultAsync(r => r.IdRequisicion == idRequisicion);
                if (req == null) return (false, "No se encontró la requisición.");

                req.IdEstatus = ESTATUS_RECHAZADA_ALMACEN;
                req.FechaModificacion = DateTime.Now;
                _dbContext.TblRequisicions.Update(req);

                await _repoBitacora.Crear(new TblBitacoraEstatus
                {
                    IdRequisicion = req.IdRequisicion,
                    IdEstatus = ESTATUS_RECHAZADA_ALMACEN,
                    FechaEstatus = DateTime.Now,
                    Observacion = motivo,
                    IdUsuario = idUsuario
                });

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "No se pudo rechazar. " + ex.Message);
            }
        }

        public async Task<(bool ok, string? error)> AnularMovimientoInventario(int idMovimiento, int idUsuario, string motivoAnulacion)
        {
            motivoAnulacion = (motivoAnulacion ?? "").Trim();
            if (string.IsNullOrWhiteSpace(motivoAnulacion)) return (false, "El motivo de anulación es obligatorio.");

            await using var tx = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var mov = await _dbContext.Set<TblMovimientoInventario>()
                    .FirstOrDefaultAsync(m => m.Id == idMovimiento);
                if (mov == null) return (false, "No se encontró el movimiento.");
                if (mov.Anulado) return (false, "El movimiento ya está anulado.");

                var inv = await _dbContext.TblInventarios.FirstOrDefaultAsync(i => i.Id == mov.IdInventario);
                if (inv == null) return (false, "No se encontró el inventario asociado.");

                // Revertir efecto en stock
                if (mov.TipoMovimiento == "E")
                {
                    inv.Existencia += mov.Cantidad;
                }
                else if (mov.TipoMovimiento == "I")
                {
                    if (inv.Existencia < mov.Cantidad) return (false, "No se puede anular: el stock quedaría negativo.");
                    inv.Existencia -= mov.Cantidad;
                }

                mov.Anulado = true;
                mov.MotivoAnulacion = motivoAnulacion;
                mov.FechaAnulacion = DateTime.Now;
                mov.IdUsuarioAnula = idUsuario;

                _dbContext.TblInventarios.Update(inv);
                _dbContext.Set<TblMovimientoInventario>().Update(mov);

                await _dbContext.SaveChangesAsync();
                await tx.CommitAsync();
                return (true, null);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return (false, "No se pudo anular el movimiento. " + ex.Message);
            }
        }
    }
}
