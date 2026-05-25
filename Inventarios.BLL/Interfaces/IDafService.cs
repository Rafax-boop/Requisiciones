namespace Inventario.BLL.Interfaces
{
    public interface IDafService
    {
        Task<bool> AutorizarRequisicion(int idRequisicion, int idUsuario, string? observaciones);
        Task<bool> AutorizarConsolidada(int idConsolidada, int idUsuario, string? observaciones);
        Task<bool> SolicitarModificacionRequisicion(int idRequisicion, int idUsuario, string observaciones);
        Task<bool> SolicitarModificacionConsolidada(int idConsolidada, int idUsuario, string observaciones);
        Task<bool> RechazarRequisicion(int idRequisicion, int idUsuario, string motivo);
        Task<bool> RechazarConsolidada(int idConsolidada, int idUsuario, string motivo);
    }
}
