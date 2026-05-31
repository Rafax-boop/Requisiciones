namespace Inventario.BLL.DTO
{
    /// <summary>
    /// Modelo editable para el formato "Gastos por pagar y/o Comprobación de Gastos".
    /// Los campos que no existen en la base de datos se capturan en el formulario.
    /// </summary>
    public class OrdenPagoEditableDTO
    {
        public int IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public string NumeroRequisicion { get; set; } = "";

        // Encabezado
        public string Fecha { get; set; } = "";
        public string AreaSolicitante { get; set; } = "";
        public string PlenaJustificacion { get; set; } = "";

        // Datos de transferencia
        public string NombreTransferencia { get; set; } = "";
        public string NumeroApi { get; set; } = "";
        public string ClabeInterbancaria { get; set; } = "";
        public string InstitucionBancaria { get; set; } = "";

        // Conceptos en factura
        public List<OrdenPagoConceptoDTO> Conceptos { get; set; } = new();

        // Totales / contrato / retenciones
        public string NumeroContrato { get; set; } = "";
        public string MontoTotalContrato { get; set; } = "";
        public string TipoRecurso { get; set; } = "";
        public string ImporteADevengar { get; set; } = "";
        public string Total { get; set; } = "";
        public string RetencionIsr { get; set; } = "";
        public string RetencionCincoAlMillar { get; set; } = "";
        public string TotalAPagar { get; set; } = "";

        // Firmas (nombre + cargo)
        public string Elaboro { get; set; } = "";
        public string Reviso { get; set; } = "";
        public string Autorizo { get; set; } = "";
        public string VistoBueno { get; set; } = "";
    }

    public class OrdenPagoConceptoDTO
    {
        public string Ff { get; set; } = "";
        public string Ua { get; set; } = "";
        public string Proyecto { get; set; } = "";
        public string Cog { get; set; } = "";
        public string Factura { get; set; } = "";
        public string Proveedor { get; set; } = "";
        public string Concepto { get; set; } = "";
        public string Subtotal { get; set; } = "";
        public string Iva { get; set; } = "";
        public string Total { get; set; } = "";
    }
}
