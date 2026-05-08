using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblAdjudicacion
{
    public int Id { get; set; }

    public string? Tipo { get; set; }

    public decimal? MontoMin { get; set; }

    public decimal? MontoMax { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
