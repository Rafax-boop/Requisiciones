using Inventario.BLL.DTO;

namespace Inventario.BLL.Interfaces
{
    public interface IAlmacenService
    {
        // ─── Consultas ───────────────────────────────────────────
        Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen();
        Task<List<InventarioItemDTO>> ObtenerInventario();
        Task<List<string>> ObtenerEstatus();
        Task<List<StockPartidasDTO>> ConsultarStockParaRequisicion(int idRequisicion);
        Task<List<RequisicionMaestraDTO>> ListarPedidosEstatus7();
        Task<List<RequisicionMaestraDTO>> ListarRequisicionesConDocumentos();
        Task<List<DocumentoExpedienteDTO>> ObtenerDocumentosPorRequisicion(int idRequisicion);

        // ─── Operaciones ─────────────────────────────────────────
        Task<bool> RegistrarIngresoInventario(IngresoInventarioDTO dto);
        Task<bool> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo);
        Task<bool> ProcesarRequisicion(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> entregas,
            IEnumerable<(int idRequisicionDetalle, int cantidadComprar)> compras);

        //ENTREGAS
        Task<List<EntregaPendienteDTO>> ListarEntregasPendientes();
        Task<int> GenerarFormatoSalida(int idRequisicion, int idUsuario);
        Task<bool> ConfirmarEntrega(int idRequisicion, List<int> idsMovimientos, int idUsuario, string rutaArchivoFirmado);

        //ENTRADAS
        Task<int> GenerarFormatoEntrada(int idRequisicion, int idUsuario);
        Task<List<PartidaCompraEntradaDTO>> ObtenerPartidasCompraParaEntrada(int idRequisicion);

        Task GuardarBorradorIngreso(int idRequisicion, int idUsuario, List<CantidadRecibidaDTO> cantidades);
        Task<List<PartidaCompraEntradaDTO>> ObtenerBorradorIngreso(int idRequisicion);
        Task<bool> ConfirmarIngresoPedido(int idRequisicion, int idUsuario, string rutaArchivoFirmado);
    }
}