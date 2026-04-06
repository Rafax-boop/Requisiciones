using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRequisicionDetalleMovimiento
{
    public int IdMovimiento { get; set; }

    public int IdRequisicion { get; set; }

    public int IdRequisicionDetalle { get; set; }

    public string TipoMovimiento { get; set; } = null!;

    public decimal CantidadOriginal { get; set; }

    public decimal CantidadMovimiento { get; set; }

    public DateTime FechaMovimiento { get; set; }

    public int IdUsuario { get; set; }

    public string? Observacion { get; set; }

    public bool? Confirmado { get; set; }

    public DateTime? FechaConfirmacion { get; set; }

    public int? IdUsuarioConfirmacion { get; set; }

    public string? FormatoSalidaFirmado { get; set; }

    public virtual TblRequisicionDetalle IdRequisicionDetalleNavigation { get; set; } = null!;

    public virtual TblRequisicion IdRequisicionNavigation { get; set; } = null!;
}
