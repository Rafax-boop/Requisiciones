namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMInventarioItem
    {
        public string? Clave { get; set; }
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Existencia { get; set; }
        public int Minimo { get; set; }
        public string Situacion { get; set; } = "";
    }
}
