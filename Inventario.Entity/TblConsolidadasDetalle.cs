using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblConsolidadasDetalle
{
    public int ConsolidadaDetalleId { get; set; }

    public int ConsolidadaId { get; set; }

    public int IdRequisicion { get; set; }

    public DateTime FechaAgregada { get; set; }

    public int? AgregadoPor { get; set; }

    public virtual TblConsolidada Consolidada { get; set; } = null!;

    public virtual TblRequisicion IdRequisicionNavigation { get; set; } = null!;
}
