using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class EntregaPendienteDTO
    {
        public int IdRequisicion { get; set; }
        public string NumRequi { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public string Departamento { get; set; }
        public string Responsable { get; set; }
        public List<ArticuloEntregaDTO> Articulos { get; set; }
        public bool EsConsolidada { get; set; }
        public int? ConsolidadaId { get; set; }
        public List<int> IdsRequisiciones { get; set; } = new();
    }

    public class ArticuloEntregaDTO
    {
        public int IdMovimiento { get; set; }
        public int IdRequisicionDetalle { get; set; }
        public int? IdArticulo { get; set; }
        public int? NumPartida { get; set; }
        public string? ClaveMaterial { get; set; }
        public string Descripcion { get; set; }
        public string UnidadMedida { get; set; }
        public decimal? CantidadOriginal { get; set; }
        public decimal? CantidadMovimiento { get; set; }
        public bool Confirmado { get; set; }
    }
}
