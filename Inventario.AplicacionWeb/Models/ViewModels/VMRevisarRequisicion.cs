namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRevisarRequisicion
    {
        public int IdRequisicion { get; set; }
        public List<IFormFile>? Factura { get; set; }
    }
}
