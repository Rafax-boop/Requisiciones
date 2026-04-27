using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRequisicionDetalleMunicipio
{
    public int IdDetalleMunicipio { get; set; }

    public int IdRequisicionDetalle { get; set; }

    public int IdRequisicion { get; set; }

    public int IdMunicipio { get; set; }

    public decimal Cantidad { get; set; }

    public DateTime FechaRegistro { get; set; }

    public virtual TblMunicipio IdMunicipioNavigation { get; set; } = null!;

    public virtual TblRequisicionDetalle IdRequisicionDetalleNavigation { get; set; } = null!;

    public virtual TblRequisicion IdRequisicionNavigation { get; set; } = null!;
}
