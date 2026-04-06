using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IDAFService
    {
        Task<List<RequisicionMaestraDTO>> ListarRequisiciones();
        Task<bool> RevisarRequisicion(int idRequisicion, int idUsuario);
    }
}
