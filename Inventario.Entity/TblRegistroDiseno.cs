using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRegistroDiseno
{
    public int Id { get; set; }

    public int? IdRequisicion { get; set; }

    public string? Ruta { get; set; }

    public DateTime? FechaSubida { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }
}
