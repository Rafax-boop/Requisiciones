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
        Task<bool> CrearRequisicion(FormularioRequisicionDTO modelo, int idUsuario);
        Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int idDepartamento);
        Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro);
        Task<RequisicionCompletaDTO?> ObtenerRequisicionCompletaPorId(int idRequisicion);
    }
}
