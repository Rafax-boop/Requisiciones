namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRequisicionMaestra
    {
        public int IdRequi { get; set; }
        public string? NumRequi { get; set; }
        public DateTime? FechaEmision { get; set; }
        public string? Departamento { get; set; }
        public string? Responsable { get; set; }
        public string? Estatus { get; set; }
    }
}
