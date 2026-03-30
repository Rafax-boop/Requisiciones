using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class DetallesRequiDTO
    {
        public bool Donativo { get; set; }
        public string?  TipoServicio { get; set; }
        public int? IdPp { get; set; }
        public string? Ff { get; set; }
        public string? TipoPrograma { get; set; }
        public int? ClaveRegion { get; set; }
        public string? Observaciones { get; set; }
        public List<DetalleArticuloDTO> Articulos { get; set; }
        public List<string> Fotos { get; set; } = new();
        public List<ArchivoAtencionDTO> Cotizaciones { get; set; } = new();
        public List<ArchivoAtencionDTO> CuadroComparativo { get; set; } = new();
    }

    public class DetalleArticuloDTO
    {
        public int IdRequisicionDetalle { get; set; }
        public int? NumPartida { get; set; }
        public int? ClaveMaterial { get; set; }
        public int? IdArticulo { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
        public string? DescripcionDetallada { get; set; }

        public string? TipoProgramacion { get; set; }
        public int? Llenado1 { get; set; }
        public int? Llenado2 { get; set; }
        public int? Llenado3 { get; set; }
        public int? Llenado4 { get; set; }
        public int? Llenado5 { get; set; }
        public int? Llenado6 { get; set; }
        public int? Llenado7 { get; set; }
        public int? Llenado8 { get; set; }
        public int? Llenado9 { get; set; }
        public int? Llenado10 { get; set; }
        public int? Llenado11 { get; set; }
        public int? Llenado12 { get; set; }
    }

    public class ArchivoAtencionDTO
    {
        public string Ruta { get; set; }
        public string NombreArchivo { get; set; }
    }
}
