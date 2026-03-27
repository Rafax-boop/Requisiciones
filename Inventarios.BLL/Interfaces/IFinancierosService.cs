using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IFinancierosService
    {
        Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idUsuarioFinancieros = null);
        Task<bool> AsignarRequisicion(int idRequi, int idUsuario, int idUsuarioFinan);
        Task<bool> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario);
    }
}
