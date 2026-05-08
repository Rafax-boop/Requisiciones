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

    public class DescargaArchivosRequisicionDTO
    {
        public string? NumRequisicion { get; set; }
        public string? NumApi { get; set; }
        public string? NumPedido { get; set; }
    }
}
