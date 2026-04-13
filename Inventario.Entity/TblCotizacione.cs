using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblCotizacione
{
    public int IdCotizacion { get; set; }

    public int? IdRequisicion { get; set; }

    public int? IdProveedor { get; set; }

    public decimal? Importe { get; set; }

    public virtual TblProvedor? IdProveedorNavigation { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }
}
