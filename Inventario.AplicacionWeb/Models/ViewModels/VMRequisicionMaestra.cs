namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRequisicionMaestra
    {
        public int IdRequi { get; set; }
        public string? NumRequi { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string? Departamento { get; set; }
        public string? Responsable { get; set; }
        public string? Estatus { get; set; }
        public int CantidadPartidas { get; set; }
        public int DiasAsignado { get; set; }
        public string? NombreAsignado { get; set; }
    }
}
