using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class MunicipiosDTO
    {
        public int Id { get; set; }
        public string Municipio { get; set; }
    }

    public class MunicipioItemDTO
    {
        public int IdMunicipio { get; set; }
        public decimal Cantidad { get; set; }
    }

    public class CantidadRecibidaDTO
    {
        public int IdRequisicionDetalle { get; set; }
        public decimal CantidadRecibida { get; set; }
    }

    public class AtenderResultadoDTO
    {
        public bool Exito { get; set; }
        public string? NumApi { get; set; }
        public string? NumPedido { get; set; }
    }
}
