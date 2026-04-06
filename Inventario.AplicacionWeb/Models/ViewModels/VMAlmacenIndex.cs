using Inventario.BLL.DTO;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    /// <summary>
    /// ViewModel para la vista Index de Almacén (datos desde BD).
    /// </summary>
    public class VMAlmacenIndex
    {
        public List<VMRequisicionMaestra> Requisiciones { get; set; } = new();
        public List<VMInventarioItem> Inventario { get; set; } = new();
        /// <summary>
        /// Nombres de estatus para el filtro (desde TblEstatus).
        /// </summary>
        public List<string> Estatus { get; set; } = new();
        /// <summary>
        /// Unidades de medida distintas para el filtro de la pestaña Inventario (desde TblInventario.UnidadMedida).
        /// </summary>
        public List<string> UnidadesMedida { get; set; } = new();

        public List<EntregaPendienteDTO> EntregasPendientes { get; set; } = new();
    }
}
