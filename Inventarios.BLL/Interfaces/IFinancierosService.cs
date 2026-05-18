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
        Task<List<TablaApiHistorialDTO>> ObtenerHistorialTablaApiPorConsolidadaAsync(int idConsolidada);

        Task<PedidoVistaDTO> ObtenerPedidoEditableAsync(int idRequisicion);
        Task<byte[]> GenerarPedidoPdfAsync(PedidoVistaDTO form, string webRootPath);
        Task GuardarHistorialPedidoAsync(PedidoVistaDTO modelo, int idUsuario, string? observacion = null);
        Task<List<PedidoHistorialDTO>> ObtenerHistorialPedidoAsync(int idRequisicion);
        Task<List<PedidoHistorialDTO>> ObtenerHistorialPedidoPorConsolidadaAsync(int idConsolidada);

        // consolidada
        Task<List<ConsolidadaFinancierosDTO>> ListarConsolidadasFinancieros(int? idUsuario = null, List<int>? estatusPermitidos = null);
        Task<bool> AsignarConsolidada(int idConsolidada, int idUsuarioLog, int idUsuarioFinan);
        Task<TablaApiEditableDTO> ObtenerTablaApiEditableConsolidadaAsync(int idConsolidada);
        Task<AtenderResultadoDTO> AtenderConsolidadaFinancieros(AtenderConsolidadaDTO modelo, int idUsuario);

        Task<PedidoVistaDTO> ObtenerPedidoEditableConsolidadaAsync(int idConsolidada);
        Task GuardarHistorialPedidoConsolidadaAsync(PedidoVistaDTO modelo, int idUsuario, string? observacion = null);
        Task<(bool Success, string Message)> EnviarFinancierosConsolidadaAsync(int idConsolidada, int idUsuario, IFormFile? archivo = null, string? webRootPath = null);
        Task<bool> FinalizarRequisicionConsolidada(int idConsolidada, List<IFormFile>? transferencias, int idUsuario);
        Task<bool> RebotarDocumentosConsolidada(int idConsolidada, string observaciones, List<string> docsObservados, int idUsuario);
    }
}
