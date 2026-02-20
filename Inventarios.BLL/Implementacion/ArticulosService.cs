using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
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

        public async Task<List<TblArticulo>> BuscarArticulos(string termino)
        {
            IQueryable<TblArticulo> query = await _repositoryArticulo.Consultar();

            if (!string.IsNullOrWhiteSpace(termino))
            {
                query = query.Where(a => a.Descripcion.Contains(termino));
            }

            return query.Take(10).ToList();
        }
    }
}
