using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblDonacion
{
    public int IdDonacion { get; set; }

    public int IdFormato { get; set; }

    public string? Motivo { get; set; }

    public DateTime FechaIngreso { get; set; }

    public int IdUsuario { get; set; }

    public virtual TblFormato IdFormatoNavigation { get; set; } = null!;

    public virtual ICollection<TblDonacionDetalle> TblDonacionDetalles { get; set; } = new List<TblDonacionDetalle>();
}
