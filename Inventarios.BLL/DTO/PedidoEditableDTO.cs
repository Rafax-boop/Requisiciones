namespace Inventario.BLL.DTO
{
    public class PedidoVistaDTO
    {
        public int? IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public string NumRequisicion { get; set; } = "";
        public string ProveedorNombre { get; set; } = "";
        public string ProveedorDireccion { get; set; } = "";
        public string ProveedorRfc { get; set; } = "";
        public string Departamento { get; set; } = "";
        public string Responsable { get; set; } = "";
        public string LugarEntrega { get; set; } = "";
        public string PartidaPresupuestal { get; set; } = "";
        public List<PedidoPartidaVistaDTO> Partidas { get; set; } = new();

        // Campos editables del encabezado
        public string NumeroPedido { get; set; } = "";
        public string NumeroPedidoEstatal { get; set; } = "";
        public string NumeroPedidoFederal { get; set; } = "";
        public string TipoRecurso { get; set; } = "";
        public string TiempoEntrega { get; set; } = "15 DIAS NATURALES POSTERIORES A LA FIRMA DEL PEDIDO";
        public string CondicionesPago { get; set; } = "30 DÍAS NAT POST A LA PRES. DE LA FACT";

        // Totales editables
        public decimal Suma { get; set; }
        public decimal Iva { get; set; }
        public decimal Descuento { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Retencion { get; set; }
        public decimal Total { get; set; }

        public decimal SumaEstatal { get; set; }
        public decimal SumaFederal { get; set; }
        public decimal IvaEstatal => SumaEstatal * 0.16m;
        public decimal IvaFederal => SumaFederal * 0.16m;
        public decimal SubtotalEstatal => SumaEstatal + IvaEstatal;
        public decimal SubtotalFederal => SumaFederal + IvaFederal;
        public decimal RetencionEstatal => AplicaRetencion ? SumaEstatal * 0.005m : 0m;
        public decimal RetencionFederal => 0m;
        public bool AplicaRetencion { get; set; }
    }

    public class PedidoPartidaVistaDTO
    {
        public string Numero { get; set; } = "";
        public string Clave { get; set; } = "";
        public string Descripcion { get; set; } = "";
        public decimal Cantidad { get; set; }
        public string UnidadMedida { get; set; } = "";
        public decimal PrecioUnitario { get; set; }
        public bool TieneIva { get; set; }
        public bool? EsEstatal { get; set; }
    }
}
