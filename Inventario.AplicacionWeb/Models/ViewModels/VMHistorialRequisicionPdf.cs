namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMHistorialRequisicionPdf
    {
        public int IdRequisicion { get; set; }
        public string TipoRequisicion { get; set; } = "Materiales";
        public string? Folio { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public string? Departamento { get; set; }
        public string? Responsable { get; set; }
        public int IdEstatus { get; set; }
        public string? EstatusActual { get; set; }
        public List<VMHistorialPasoPdf> Historial { get; set; } = new();
    }

    public class VMHistorialPasoPdf
    {
        public string Departamento { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Hora { get; set; } = "";
        public string Estado { get; set; } = "";
        public string Responsable { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Nota { get; set; } = "";
    }
}
