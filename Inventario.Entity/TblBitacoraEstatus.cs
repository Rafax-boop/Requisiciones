using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblBitacoraEstatus
{
    public int IdBitacoraEstatus { get; set; }

    public int? IdEstatus { get; set; }

    public DateTime? FechaEstatus { get; set; }

    public int? IdRequisicion { get; set; }

    public string? Observacion { get; set; }

    public int? IdUsuario { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }
}
