using Inventario.DAL.DBCONTEXT;
using Inventario.DAL.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.DAL.Implementacion
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DbSigereContext _dbContext;
        private IDbContextTransaction? _transaction;

        public UnitOfWork(DbSigereContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _dbContext.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _transaction!.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await _transaction!.RollbackAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
        }
    }
}
