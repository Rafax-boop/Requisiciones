using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblPerido
{
    public int IdPeriodo { get; set; }

    public int? Anio { get; set; }

    public DateOnly? FechaInicio { get; set; }

    public DateOnly? FechaFin { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
