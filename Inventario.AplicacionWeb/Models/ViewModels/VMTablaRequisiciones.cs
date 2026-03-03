using Microsoft.AspNetCore.Mvc.Rendering;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMTablaRequisiciones
    {
        public List<VMRequisicionMaestra> Requisiciones { get; set; }

        public List<SelectListItem> ListaActividades { get; set; }

        public List<SelectListItem> ListaMunicipios { get; set; }
    }
}
