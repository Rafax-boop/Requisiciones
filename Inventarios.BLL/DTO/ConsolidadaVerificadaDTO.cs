namespace Inventario.BLL.DTO
{
    public class ConsolidadaVerificadaDTO
    {
        public int ConsolidadaId { get; set; }
        public string FolioConsolidada { get; set; }
        public string FechaCreacion { get; set; }
        public string Departamentos { get; set; }
        public int CantidadRequisiciones { get; set; }
        public int TotalPartidas { get; set; }
        public int IdEstatus { get; set; }
        public string Estatus { get; set; }
        public bool RequiServicio { get; set; }
    }
}
