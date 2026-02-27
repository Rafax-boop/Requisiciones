using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class InventarioItemDTO
    {
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Existencia { get; set; }
        public int Minimo { get; set; }
        public string Situacion { get; set; } = "";
    }
}
