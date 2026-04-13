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

    public class ProveedoresDTO
    {
        public int Id { get; set; }
        public string Proveedor { get; set; }
    }

    public class CotizacionDTO
    {
        public int IdProveedor { get; set; }
        public decimal Importe { get; set; }
    }
}
