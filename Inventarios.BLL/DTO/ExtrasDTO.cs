using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{

    public class MunicipioItemDTO
    {
        public int IdMunicipio { get; set; }
        public decimal Cantidad { get; set; }
    }

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
        public int? IdPartida { get; set; }
        public string NombrePartida { get; set; }
        public bool? IVA { get; set; }
    }

    public class PartidaDTO
    {
        public int IdRequiDetalle { get; set; }
        public int? NumPartida { get; set; }
        public string NombrePartida { get; set; }
    }

    public class CantidadRecibidaDTO
    {
        public int IdRequisicionDetalle { get; set; }
        public decimal CantidadRecibida { get; set; }
    }

    public class ProveedorGanadorDTO
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public bool SeleccionManual { get; set; }
    }

    // Para la respuesta al wizard (proveedores que cotizaron con sus totales)
    public class OpcionProveedorDTO
    {
        public int IdProveedor { get; set; }
        public string NombreProveedor { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Iva { get; set; }
        public decimal Total { get; set; }
        public bool EsSugerido { get; set; } // true = menor costo
    }

    public class AtenderResultadoDTO
    {
        public bool Exito { get; set; }
        public string? NumApi { get; set; }
        public string? NumPedido { get; set; }
    }
}
