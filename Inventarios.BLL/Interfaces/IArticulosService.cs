using Inventario.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IArticulosService
    {
        Task<TblArticulo> ObtenerArticuloPorId(int idArticulo);
        Task<List<TblArticulo>> BuscarArticulos(string termino, bool mensual);
        Task<List<int>> BuscarCogs(string termino);
    }
}
