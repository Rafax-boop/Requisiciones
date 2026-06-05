namespace Inventario.BLL.DTO
{
    public class DonacionResumenDTO
    {
        public int IdDonacion { get; set; }
        public int NumFormato { get; set; }
        public DateTime FechaIngreso { get; set; }
        public string Motivo { get; set; } = "";
        public int CantidadArticulos { get; set; }
        public string RutaFirmado { get; set; } = "";
    }

    public class DonacionDetalleItemDTO
    {
        public string? Clave { get; set; }
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Cantidad { get; set; }
    }
}
