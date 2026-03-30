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

        // ─── Operaciones ─────────────────────────────────────────
        Task<bool> RegistrarIngresoInventario(IngresoInventarioDTO dto);
        Task<bool> AprobarRequisicionCompleta(int idRequisicion, int idUsuario);
        Task<bool> AprobarRequisicionParcial(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> partidas);
        Task<bool> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo);
        Task<bool> ProcesarRequisicion(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> entregas,
            IEnumerable<(int idRequisicionDetalle, int cantidadComprar)> compras);
    }
}