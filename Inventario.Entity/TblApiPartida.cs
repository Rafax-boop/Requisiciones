using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblApiPartida
{
    public int IdApiPartida { get; set; }

    public int IdHistorial { get; set; }

    public int? IdRequisicion { get; set; }

    public int? IdConsolidada { get; set; }

    public string? NumeroPartida { get; set; }

    public string? Ua { get; set; }

    public string? Region { get; set; }

    public string? ClaveMunicipio { get; set; }

    public string? ClaveFuente { get; set; }

    public string? Pp { get; set; }

    public string? Componente { get; set; }

    public string? Actividad { get; set; }

    public string? ObjetoGasto { get; set; }

    public decimal? ImporteSolicitado { get; set; }

    public decimal? ImporteAutorizado { get; set; }

    public bool? EsEstatal { get; set; }

    public virtual TblConsolidada? IdConsolidadaNavigation { get; set; }

    public virtual TblTablaApiHistorial IdHistorialNavigation { get; set; } = null!;

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }
}
