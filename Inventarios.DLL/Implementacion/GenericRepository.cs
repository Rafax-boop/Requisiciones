using Inventario.DAL.DBCONTEXT;
using Inventario.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.DAL.Implementacion
{
    public class GenericRepository<TEntity> : IGenericRepository<TEntity> where TEntity : class
    {
        private readonly DbSigereContext _dbContext;

        public GenericRepository(DbSigereContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IQueryable<TEntity>> Consultar(Expression<Func<TEntity, bool>> filtro = null)
        {
            IQueryable<TEntity> queryEntidad = filtro == null ? _dbContext.Set<TEntity>() : _dbContext.Set<TEntity>().Where(filtro);
            return queryEntidad;
        }

        public async Task<TEntity> Crear(TEntity entidad)
        {
            _dbContext.Set<TEntity>().Add(entidad);
            await _dbContext.SaveChangesAsync();
            return entidad;
        }

        public async Task<IEnumerable<TEntity>> CrearRango(IEnumerable<TEntity> entidades)
        {
            try
            {
                _dbContext.Set<TEntity>().AddRange(entidades);
                await _dbContext.SaveChangesAsync();
                return entidades;
            }
            catch (Exception ex)
            {
                throw new Exception("Error al crear las entidades", ex);
            }
        }

        public async Task<bool> Editar(TEntity entidad)
        {
            try
            {
                _dbContext.Update(entidad);
                await _dbContext.SaveChangesAsync();
                    return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Eliminar(TEntity entidad)
        {
            try
            {
                _dbContext.Remove(entidad);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<TEntity> Obtener(Expression<Func<TEntity, bool>> filtro)
        {
            try
            {
                TEntity entidad = await _dbContext.Set<TEntity>().FirstOrDefaultAsync(filtro);
                return entidad;
            }
            catch
            {
                throw;
            }
        }
    }
}
