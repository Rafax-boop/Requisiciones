using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventario.BLL.DTO;
using Inventario.Entity;

namespace Inventario.BLL.Interfaces
{
    public interface IRequisicionesService
    {
        Task<bool> CrearRequisicion(FormularioRequisicionDTO modelo);
        Task<List<RequisicionMaestraDTO>> ListarRequisiciones();
        Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro);
    }
}
