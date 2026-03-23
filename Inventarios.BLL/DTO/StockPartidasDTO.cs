namespace Inventario.BLL.DTO
{
    public class StockPartidasDTO
    {
        public int IdRequisicionDetalle { get; set; }
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int CantidadSolicitada { get; set; }
        public int StockDisponible { get; set; }
        public bool ExisteEnInventario { get; set; }
    }
}
