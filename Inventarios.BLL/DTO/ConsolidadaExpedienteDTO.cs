namespace Inventario.BLL.DTO
{
    public class ConsolidadaExpedienteDTO
    {
        public int ConsolidadaId { get; set; }
        public string FolioConsolidada { get; set; }
        public int IdEstatus { get; set; }
        public string Estatus { get; set; }
        public string FechaCreacion { get; set; }
        public int? IdPp { get; set; }
        public string? Ff { get; set; }
        public string? TipoPrograma { get; set; }
        public string? NumeroApi { get; set; }

        public List<RequiHijaDTO> Requisiciones { get; set; } = new();
        public List<DetalleArticuloDTO> Articulos { get; set; } = new();

        public List<ArchivoAtencionDTO> CuadroComparativo { get; set; } = new();
        public List<ArchivoAtencionDTO> Anexos { get; set; } = new();

        public List<ArchivoAtencionDTO> ArchivosSiaf { get; set; } = new();
        public List<ArchivoAtencionDTO> ArchivosTablaApi { get; set; } = new();
        public List<ArchivoAtencionDTO> ArchivosPedidoCompra { get; set; } = new();

        public List<ArchivoAtencionDTO> DocumentosProveedor { get; set; } = new();
        public string? Observaciones { get; set; }
    }
}
