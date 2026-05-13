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
        public DateTime? FechaModificacion { get; set; }
        public string? Departamento { get; set; }
        public string? Responsable { get; set; }
        public int IdEstatus { get; set; }
        public string? Estatus { get; set; }
        public int CantidadPartidas { get; set; }
        public int DiasAsignado { get; set; }
        public string? NombreAsignado { get; set; }
        public bool? RequiServicio { get; set; }
        public string? ComentarioRechazo { get; set; }
        public int? ConsolidadaId { get; set; }
    }
}
