using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblPrioridad
{
    public int IdPrioridad { get; set; }

    public string? NombrePrioridad { get; set; }

    public short? DiasAtencion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
