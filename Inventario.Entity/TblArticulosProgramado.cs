using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblArticulosProgramado
{
    public int Id { get; set; }

    public int? IdRequisicion { get; set; }

    public int? IdRequisicionDetalle { get; set; }

    public int? IdArticulo { get; set; }

    public string? TipoProgramacion { get; set; }

    public int? Llenado1 { get; set; }

    public int? Llenado2 { get; set; }

    public int? Llenado3 { get; set; }

    public int? Llenado4 { get; set; }

    public int? Llenado5 { get; set; }

    public int? Llenado6 { get; set; }

    public int? Llenado7 { get; set; }

    public int? Llenado8 { get; set; }

    public int? Llenado9 { get; set; }

    public int? Llenado10 { get; set; }

    public int? Llenado11 { get; set; }

    public int? Llenado12 { get; set; }

    public virtual TblArticulo? IdArticuloNavigation { get; set; }

    public virtual TblRequisicionDetalle? IdRequisicionDetalleNavigation { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }
}
