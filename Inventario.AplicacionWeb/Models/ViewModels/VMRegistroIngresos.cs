namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMIngresoResumen
    {
        public int PrimerIdIngreso { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Motivo { get; set; } = "";
        public int CantidadArticulos { get; set; }
        public string? RutaFirmado { get; set; }
        public bool TienePdfFirmado => !string.IsNullOrEmpty(RutaFirmado);
    }

    public class VMRegistroIngresosIndex
    {
        public List<VMIngresoResumen> Ingresos { get; set; } = new();
        public List<VMInventarioItem> Inventario { get; set; } = new();
        public List<string> UnidadesMedida { get; set; } = new();
    }
}
