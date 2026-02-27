using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class RequisicionMaestraDTO
    {
        public int IdRequi { get; set; }
        public string? NumRequi { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public string? Departamento { get; set; }
        public string? Responsable { get; set; }
        public string? Estatus { get; set; }
        public int CantidadPartidas { get; set; }
    }
}
