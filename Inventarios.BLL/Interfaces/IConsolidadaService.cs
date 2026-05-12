using Inventario.BLL.DTO;
using Inventario.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IConsolidadaService
    {
        Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConsolidables(int idUsuario);
        Task<TblConsolidada> CrearConsolidada(List<int> idsRequisiciones, int idUsuario);
    }
}
