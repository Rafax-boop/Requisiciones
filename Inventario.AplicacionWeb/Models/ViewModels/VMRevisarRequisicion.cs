using Microsoft.AspNetCore.Http;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRevisarRequisicion
    {
        public int IdRequisicion { get; set; }
    }

    public class FinalizarRequisicionRequestDto
    {
        public int? IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public List<IFormFile>? Transferencia { get; set; }
    }
}
