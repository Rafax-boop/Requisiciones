using Inventario.DAL.DBCONTEXT;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.DAL.Implementacion
{
    public class RequisicionRepository : GenericRepository<TblRequisicion>, IRequisicionRepository
    {
        private readonly DbSigereContext _dbContext;

        public RequisicionRepository(DbSigereContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<TblRequisicion> CrearConFolio(TblRequisicion requisicion)
        {
            // Si ya hay una transacción activa (viene del UnitOfWork), la reutiliza
            // Si no hay ninguna, crea una propia
            var transaccionExistente = _dbContext.Database.CurrentTransaction;
            var transaction = transaccionExistente ??
                              await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                int anio = DateTime.Now.Year;
                var ultimoConsecutivo = await _dbContext.TblRequisicions
                    .Where(r => r.FechaModificacion.HasValue && r.FechaModificacion.Value.Year == anio)
                    .MaxAsync(r => (int?)r.Consecutivo) ?? 0;

                requisicion.Consecutivo = ultimoConsecutivo + 1;
                var tipo = requisicion.RequiServicio == true ? "SER" : "ADQ";
                requisicion.NumRequisicion = $"REQ-{tipo}-{anio}-{(ultimoConsecutivo + 1):D4}";

                _dbContext.TblRequisicions.Add(requisicion);
                await _dbContext.SaveChangesAsync();

                // Solo hace commit si fue él quien abrió la transacción
                if (transaccionExistente == null)
                    await transaction.CommitAsync();

                return requisicion;
            }
            catch
            {
                // Solo hace rollback si fue él quien abrió la transacción
                if (transaccionExistente == null)
                    await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
