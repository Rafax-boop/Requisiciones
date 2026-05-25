using Inventario.BLL.DTO;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMTablaRequisiciones
    {
        public List<VMRequisicionMaestra> Requisiciones { get; set; } = new();
        public List<ConsolidadaDTO> Consolidadas { get; set; } = new();
        public List<SelectListItem> ListaActividades { get; set; } = new();
        public List<SelectListItem> ListaFuentesFinanciamiento { get; set; } = new();
        public List<SelectListItem> ListaProveedores { get; set; } = new();
        public List<string> Estatus { get; set; } = new();
    }
}
