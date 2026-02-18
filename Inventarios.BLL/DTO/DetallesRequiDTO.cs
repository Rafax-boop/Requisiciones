using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class DetallesRequiDTO
    {
        public List<DetalleArticuloDTO> Articulos { get; set; }
    }

    public class DetalleArticuloDTO
    {
        public int? NumPartida { get; set; }
        public int? IdArticulo { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
    }
}
