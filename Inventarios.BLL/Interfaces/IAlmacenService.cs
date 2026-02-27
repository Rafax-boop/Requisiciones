using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IAlmacenService
    {
        Task<List<RequisicionMaestraDTO>> ListarRequisicionesAutorizadas();
        Task<List<InventarioItemDTO>> ObtenerInventario();
        Task<List<string>> ObtenerEstatus();
    }
}
