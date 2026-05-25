using Inventario.BLL.DTO;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class GuardarCotizacionesRequest
    {
        public int IdRequisicion { get; set; }
        public List<CotizacionDTO> Cotizaciones { get; set; } = new();
    }

    public class GuardarGanadorRequest
    {
        public int IdRequisicion { get; set; }
        public int IdProveedor { get; set; }
        public bool SeleccionManual { get; set; }
        public string? Justificacion { get; set; }
    }

}
