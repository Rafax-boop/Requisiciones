namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class AprobarRequest
    {
        public int IdRequisicion { get; set; }
    }

    public class AprobarParcialRequest
    {
        public int IdRequisicion { get; set; }
        public List<AprobarParcialPartidaRequest>? Partidas { get; set; }
    }

    public class AprobarParcialPartidaRequest
    {
        public int IdRequisicionDetalle { get; set; }
        public int CantidadAprobada { get; set; }
    }

    public class RechazarRequest
    {
        public int IdRequisicion { get; set; }
        public string? Motivo { get; set; }
    }

    public class ProcesarRequisicionRequest
    {
        public int IdRequisicion { get; set; }
        public List<PartidaEntregaRequest>? Entregas { get; set; }
        public List<PartidaCompraRequest>? Compras { get; set; }
    }

    public class PartidaEntregaRequest
    {
        public int IdRequisicionDetalle { get; set; }
        public int CantidadAprobada { get; set; }
    }

    public class PartidaCompraRequest
    {
        public int IdRequisicionDetalle { get; set; }
        public int CantidadComprar { get; set; }
    }
}