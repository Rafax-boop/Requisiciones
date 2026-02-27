using Inventario.BLL.Interfaces;
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
    public class ArticulosService : IArticulosService
    {
        private readonly IGenericRepository<TblArticulo> _repositoryArticulo;

        public ArticulosService(IGenericRepository<TblArticulo> repositoryArticulo)
        {
            _repositoryArticulo = repositoryArticulo;
        }

        public async Task<TblArticulo> ObtenerArticuloPorId(int idArticulo)
        {
            return await _repositoryArticulo.Obtener(a => a.Id == idArticulo);
        }

        public async Task<List<TblArticulo>> BuscarArticulos(string termino, bool mensual)
        {
            IQueryable<TblArticulo> query;

            if (mensual)
                query = await _repositoryArticulo.Consultar(a => a.Cog > 1999 && a.Cog < 3000);
            else
                query = await _repositoryArticulo.Consultar();

            if (!string.IsNullOrWhiteSpace(termino))
                query = query.Where(a => a.Descripcion.Contains(termino));

            return await query.Take(10).ToListAsync();
        }

        public async Task<List<int>> BuscarCogs(string termino)
        {
            var query = await _repositoryArticulo.Consultar();

            if (!string.IsNullOrWhiteSpace(termino) && int.TryParse(termino, out int cogNum))
                query = query.Where(a => a.Cog.ToString().Contains(termino));
            else if (!string.IsNullOrWhiteSpace(termino))
                query = query.Where(a => a.Cog.ToString().Contains(termino));

            return await query
                .Select(a => a.Cog)
                .Distinct()
                .OrderBy(c => c)
                .Take(10)
                .ToListAsync();
        }
    }
}
