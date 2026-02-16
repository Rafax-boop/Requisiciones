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
        Task<TblArticulo1> ObtenerArticuloPorId(int idArticulo);
        Task<List<TblArticulo1>> BuscarArticulos(string termino);
    }
}
