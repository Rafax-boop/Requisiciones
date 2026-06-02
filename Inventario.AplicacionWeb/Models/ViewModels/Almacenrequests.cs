using Inventario.BLL.DTO;
using Microsoft.AspNetCore.Http;

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
        public bool EnviarCorreo { get; set; } = true;
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

    public class AnularMovimientoRequest
    {
        public int IdMovimiento { get; set; }
        public string? Motivo { get; set; }
    }

    public class IngresoInventarioRequest
    {
        public string? Clave { get; set; }
        public string? Descripcion { get; set; }
        public string? UnidadMedida { get; set; }
        public int Cantidad { get; set; }
        public string? Motivo { get; set; }
    }

    public class PartidaEntrega
    {
        public int IdRequisicionDetalle { get; set; }
        public int CantidadAprobada { get; set; }
    }

    public class PartidaCompra
    {
        public int IdRequisicionDetalle { get; set; }
        public int CantidadComprar { get; set; }
    }

    public class ConfirmarEntregaRequest
    {
        public int IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public List<int> IdsMovimientos { get; set; } = new();
        public IFormFile? FormatoSalidaFirmado { get; set; }
    }

    public class RegistrarIngresoPedidoRequest
    {
        public int IdRequisicion { get; set; }
        public string? Cantidades { get; set; }
        public IFormFile? FormatoEntradaFirmado { get; set; }
    }

    public class GuardarBorradorIngresoRequest
    {
        public int IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public List<CantidadRecibidaDTO> Cantidades { get; set; } = new();
    }

    public class ConfirmarIngresoPedidoRequest
    {
        public int IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public IFormFile? FormatoEntradaFirmado { get; set; }
        public bool EnviarCorreo { get; set; } = true;
    }

    public class HistorialDocumentosVM
    {
        public int IdRequisicion { get; set; }
        public int IdConsolidada { get; set; }
        public string NumRequisicion { get; set; } = "";
        public List<TablaApiHistorialDTO> HistorialTablaApi { get; set; } = new();
        public List<PedidoHistorialDTO> HistorialPedido { get; set; } = new();
        public List<OrdenPagoHistorialDTO> HistorialOrdenPago { get; set; } = new();
    }

    public class CrearConsolidadaRequest
    {
        public List<int> IdsRequisiciones { get; set; } = new();
    }
}
