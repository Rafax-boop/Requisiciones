using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblConsolidada
{
    public int ConsolidadaId { get; set; }

    public string FolioConsolidada { get; set; } = null!;

    public int? IdAdjudicacion { get; set; }

    public int? IdPp { get; set; }

    public string? Ff { get; set; }

    public string? TipoPrograma { get; set; }

    public bool? RequiServicio { get; set; }

    public string? NumApi { get; set; }

    public string? NumPedido { get; set; }

    public int? IdUsuarioMat { get; set; }

    public int? IdUsuarioFinan { get; set; }

    public string? Hash { get; set; }

    public int IdEstatus { get; set; }

    public DateTime FechaCreacion { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int? IdUsuario { get; set; }

    public virtual TblEstatus IdEstatusNavigation { get; set; } = null!;

    public virtual TblUsuario? IdUsuarioFinanNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioMatNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<TblConsolidadasDetalle> TblConsolidadasDetalles { get; set; } = new List<TblConsolidadasDetalle>();

    public virtual ICollection<TblRegistroDiseno> TblRegistroDisenos { get; set; } = new List<TblRegistroDiseno>();

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();

    public virtual ICollection<TblTablaApiHistorial> TblTablaApiHistorials { get; set; } = new List<TblTablaApiHistorial>();

    public virtual ICollection<TblFormato> TblFormatos { get; set; } = new List<TblFormato>();
}
