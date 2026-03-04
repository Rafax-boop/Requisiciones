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
        public int? IdPp { get; set; }
        public string? Ff { get; set; }
        public string? TipoPrograma { get; set; }
        public int? ClaveRegion { get; set; }
        public List<DetalleArticuloDTO> Articulos { get; set; }
    }

    public class DetalleArticuloDTO
    {
        public int? NumPartida { get; set; }
        public int? IdArticulo { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
        public string? DescripcionDetallada { get; set; }
    }
}
