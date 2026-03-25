using Inventario.Entity;
using Inventario.Entity.Enums;
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
        Task<List<TblArticulo>> BuscarArticulos(string termino, TipoBusquedaArticulo tipo);
        Task<List<int>> BuscarCogs(string termino);
    }
}
