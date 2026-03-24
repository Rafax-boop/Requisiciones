using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRequisicionDetalle
{
    public int IdRequisicionDetalle { get; set; }

    public int IdRequisicion { get; set; }

    public int? NumPartida { get; set; }

    public int? IdArticulo { get; set; }

    public decimal? Cantidad { get; set; }

    public string? UnidadMedida { get; set; }

    public string? Descripcion { get; set; }

    public string? DescripcionDetallada { get; set; }

    public DateTime? FechaRegistro { get; set; }

    public int? CogEditable { get; set; }

    public int? IdEstatus { get; set; }

    public bool? Activo { get; set; }

    public virtual TblRequisicion IdRequisicionNavigation { get; set; } = null!;

    public virtual ICollection<TblArticulosProgramado> TblArticulosProgramados { get; set; } = new List<TblArticulosProgramado>();
}
