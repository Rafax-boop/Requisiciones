using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblTablaApiHistorial
{
    public int IdHistorial { get; set; }

    public int? IdRequisicion { get; set; }

    public int? IdConsolidada { get; set; }

    public int IdUsuario { get; set; }

    public DateTime FechaGeneracion { get; set; }

    public string DatosJson { get; set; } = null!;

    public string? Observacion { get; set; }

    public virtual TblConsolidada? IdConsolidadaNavigation { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }

    public virtual TblUsuario IdUsuarioNavigation { get; set; } = null!;

    public virtual ICollection<TblApiPartida> TblApiPartida { get; set; } = new List<TblApiPartida>();
}
