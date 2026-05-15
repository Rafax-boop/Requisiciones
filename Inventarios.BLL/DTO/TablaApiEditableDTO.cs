namespace Inventario.BLL.DTO
{
    public class TablaApiEditableDTO
    {
        public int IdRequisicion { get; set; }
        public int? IdConsolidada { get; set; }
        public string FechaElaboracion { get; set; } = "";
        public string Ejercicio { get; set; } = "";
        public string AreaSolicitanteClave { get; set; } = "";
        public string AreaSolicitanteNombre { get; set; } = "";
        public string DescripcionBienServicio { get; set; } = "";
        public string Justificacion { get; set; } = "";
        public string NumeroRequisicion { get; set; } = "";
        public string OficioSuficiencia { get; set; } = "";
        public string ContratoAsociado { get; set; } = "";
        public string Comentarios { get; set; } = "";
        public string TotalSolicitado { get; set; } = "";
        public string TotalAutorizado { get; set; } = "";
        public List<TablaApiPartidaEditableDTO> Partidas { get; set; } = new();
    }

    public class TablaApiPartidaEditableDTO
    {
        public string Numero { get; set; } = "";
        public string Ua { get; set; } = "";
        public string Region { get; set; } = "";
        public string ClaveMunicipio { get; set; } = "";
        public string ImporteSolicitado { get; set; } = "";
        public string FuenteFinanciamiento { get; set; } = "";
        public string Pp { get; set; } = "";
        public string Componente { get; set; } = "";
        public string Actividad { get; set; } = "";
        public string ObjetoGasto { get; set; } = "";
        public string ImporteAutorizado { get; set; } = "";
    }

    // Inventario.BLL.DTO/TablaApiHistorialDTO.cs
    public class TablaApiHistorialDTO
    {
        public int IdHistorial { get; set; }
        public int IdRequisicion { get; set; }
        public string NumeroRequisicion { get; set; } = "";
        public DateTime FechaGeneracion { get; set; }
        public string NombreUsuario { get; set; } = "";
        public string? Observacion { get; set; }
        public TablaApiEditableDTO? Modelo { get; set; }
    }

    public class PedidoHistorialDTO
    {
        public int IdHistorial { get; set; }
        public int IdRequisicion { get; set; }
        public DateTime? FechaGeneracion { get; set; }
        public string NombreUsuario { get; set; } = "";
        public string? Observacion { get; set; }
        public PedidoVistaDTO? Modelo { get; set; }
    }
}
