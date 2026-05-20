namespace Inventario.BLL.DTO
{
    public class ProveedoresDTO
    {
        public int Id { get; set; }
        public string Proveedor { get; set; }
    }

    public class CotizacionDTO
    {
        public int IdProveedor { get; set; }
        public decimal Importe { get; set; }
        public string NombreProveedor { get; set; }
        public int? Vigencia { get; set; }
        public int? IdPartida { get; set; }
        public int? NumPartida { get; set; }
        public string NombrePartida { get; set; }
        public string Descripcion { get; set; }
        public string DescripcionDetallada { get; set; }
        public bool? IVA { get; set; }
    }

    public class PartidaDTO
    {
        public int IdRequiDetalle { get; set; }
        public int? NumPartida { get; set; }
        public string NombrePartida { get; set; }
        public string Descripcion { get; set; }
        public string DescripcionDetallada { get; set; }
    }

    public class ProveedorGanadorDTO
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public bool SeleccionManual { get; set; }
        public string Justificacion { get; set; } = "";
    }

    public class OpcionProveedorDTO
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public bool EsSugerido { get; set; }
    }
}
