using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblUsuario
{
    public int IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string? Pasword { get; set; }

    public string? NombreEnlace { get; set; }

    public string? CargoEnlace { get; set; }

    public string? Correoenlace { get; set; }

    public string? Area { get; set; }

    public int? IdRol { get; set; }

    public bool? Activo { get; set; }

    public virtual TblRol? IdRolNavigation { get; set; }

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
