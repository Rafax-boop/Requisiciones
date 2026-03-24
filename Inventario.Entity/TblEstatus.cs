using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblEstatus
{
    public int IdEstatus { get; set; }

    public string NombreEstatus { get; set; } = null!;

    public bool Actvio { get; set; }

    public virtual ICollection<TblRequisicionDetalle> TblRequisicionDetalles { get; set; } = new List<TblRequisicionDetalle>();

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
