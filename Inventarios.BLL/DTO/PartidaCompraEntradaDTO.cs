namespace Inventario.BLL.DTO
{
    public class PartidaCompraEntradaDTO
    {
        public int IdRequisicionDetalle { get; set; }
        public int? NumPartida { get; set; }
        public int? IdArticulo { get; set; }
        public string ClaveMaterial { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal CantidadComprar { get; set; }
    }
}
