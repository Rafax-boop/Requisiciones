using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblPedido
{
    public int IdPedido { get; set; }

    public int? IdRequisicion { get; set; }

    public int? IdConsolidada { get; set; }

    public string NumPedido { get; set; } = null!;

    public string TipoRecurso { get; set; } = null!;

    public DateTime FechaGeneracion { get; set; }

    public int? IdUsuario { get; set; }

    public virtual TblConsolidada? IdConsolidadaNavigation { get; set; }

    public virtual TblRequisicion? IdRequisicionNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioNavigation { get; set; }
}
