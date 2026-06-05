using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblDonacionDetalle
{
    public int IdDetalle { get; set; }

    public int IdDonacion { get; set; }

    public int IdInventario { get; set; }

    public int Cantidad { get; set; }

    public virtual TblDonacion IdDonacionNavigation { get; set; } = null!;

    public virtual TblInventario IdInventarioNavigation { get; set; } = null!;
}
