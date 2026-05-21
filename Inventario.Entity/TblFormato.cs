using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblFormato
{
    public int IdFormato { get; set; }

    public int NumeroFormato { get; set; }

    public string TipoFormato { get; set; } = null!;

    public int? IdRequisicion { get; set; }

    public DateTime FechaFormato { get; set; }

    public int IdUsuario { get; set; }

    public string RutaArchivo { get; set; } = null!;

    public int? IdConsolidada { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }

    public virtual ICollection<TblRequisicionDetalleMovimiento> TblRequisicionDetalleMovimientos { get; set; } = new List<TblRequisicionDetalleMovimiento>();
}
