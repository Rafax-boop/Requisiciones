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
            using var transaction = await _dbContext.Database
                .BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                int anio = DateTime.Now.Year;

                var ultimoConsecutivo = await _dbContext.TblRequisicions
                    .Where(r => r.FechaSistema.HasValue && r.FechaSistema.Value.Year == anio)
                    .MaxAsync(r => (int?)r.Consecutivo) ?? 0;

                requisicion.Consecutivo = ultimoConsecutivo + 1;
                requisicion.NumRequisicion = $"REQ-{anio}-{(ultimoConsecutivo + 1):D4}";

                _dbContext.TblRequisicions.Add(requisicion);
                await _dbContext.SaveChangesAsync();

                await transaction.CommitAsync();
                return requisicion;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
