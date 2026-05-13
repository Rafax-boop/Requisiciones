using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class ConsolidadaDTO
    {
        public int ConsolidadaID { get; set; }
        public string FolioConsolidada { get; set; }
        public int CantidadRequis { get; set; }
        public int TotalPartidas { get; set; }
        public string Departamentos { get; set; }
        public string FechaCreacion { get; set; }
        public int IdEstatus { get; set; }
        public string Estatus { get; set; }
        public string CreadoPor { get; set; }
    }
}
