using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class ConsolidadaDetalleDTO
    {
        public int ConsolidadaId { get; set; }
        public string FolioConsolidada { get; set; }
        public string Estatus { get; set; }
        public string FechaCreacion { get; set; }
        public string CreadoPor { get; set; }

        public List<RequiHijaDTO> Requisiciones { get; set; } = new();
        public List<ArticuloConsolidadoDTO> Articulos { get; set; } = new();

        public List<ArchivoAtencionDTO> CuadroComparativo { get; set; } = new();
        public List<ArchivoAtencionDTO> Anexos { get; set; } = new();
    }

    public class RequiHijaDTO
    {
        public int IdRequi { get; set; }
        public string NumRequi { get; set; }
        public string Departamento { get; set; }
        public string Responsable { get; set; }
        public int CantidadPartidas { get; set; }
    }

    public class ArticuloConsolidadoDTO
    {
        public string NumRequi { get; set; }
        public int? NumPartida { get; set; }
        public decimal? Cantidad { get; set; }
        public string UnidadMedida { get; set; }
        public string Descripcion { get; set; }
        public string DescripcionDetallada { get; set; }
    }

    public class AtenderConsolidadaDTO
    {
        public int IdConsolidada { get; set; }
        public int IdPP { get; set; }
        public string FF { get; set; }
        public string TipoPrograma { get; set; }
        public string Observaciones { get; set; }
    }

    public class PartidaConsolidadaDTO
    {
        public int IdRequiDetalle { get; set; }
        public int? IdArticulo { get; set; }
        public string Descripcion { get; set; }
        public string DescripcionDetallada { get; set; }
        public decimal CantidadTotal { get; set; }
        public string UnidadMedida { get; set; }
        public int NumPartida { get; set; }
        public List<int> IdsRequiDetalle { get; set; } = new();
        public Dictionary<int, int> IdRequisicionPorDetalle { get; set; } = new();
    }
}
