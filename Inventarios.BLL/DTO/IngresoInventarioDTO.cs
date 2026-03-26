using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class IngresoInventarioDTO
    {
        public string? Clave { get; set; }
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Cantidad { get; set; }
        public int IdUsuario { get; set; }
        public string Motivo { get; set; } = "";
    }
}
