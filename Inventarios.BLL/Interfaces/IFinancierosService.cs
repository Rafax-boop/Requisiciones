using Inventario.BLL.DTO;
using Microsoft.AspNetCore.Http;
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
        Task<AtenderResultadoDTO> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario);
        Task<bool> FinalizarRequisicion(int idRequisicion, List<IFormFile>? transferencias, int idUsuario);

        //tabla api
        Task<byte[]> GenerarTablaApiAsync(int idRequisicion);
        Task<TablaApiEditableDTO> ObtenerTablaApiEditableAsync(int idRequisicion);
        Task<byte[]> GenerarTablaApiAsync(TablaApiEditableDTO modelo);
        Task GuardarHistorialTablaApiAsync(TablaApiEditableDTO modelo, int idUsuario, string? observacion = null);
        Task<List<TablaApiHistorialDTO>> ObtenerHistorialTablaApiAsync(int idRequisicion);

        // pedido
        Task<PedidoVistaDTO> ObtenerPedidoEditableAsync(int idRequisicion);
        Task<byte[]> GenerarPedidoPdfAsync(PedidoVistaDTO form, string webRootPath);
    }
}
