namespace Inventario.BLL.DTO
{
    public class ConsolidadaFinancierosDTO
    {
        public int ConsolidadaId { get; set; }
        public string FolioConsolidada { get; set; } = null!;
        public int CantidadRequis { get; set; }
        public string Departamentos { get; set; } = null!;
        public string FechaCreacion { get; set; } = null!;
        public int IdEstatus { get; set; }
        public string Estatus { get; set; } = null!;
        public int DiasAsignado { get; set; }
        public string? NombreAsignado { get; set; }
        public int? IdUsuarioFinan { get; set; }
    }
}
