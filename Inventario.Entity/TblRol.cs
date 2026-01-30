using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRol
{
    public int Id { get; set; }

    public string? Descripcion { get; set; }

    public bool? Activo { get; set; }

    public virtual ICollection<TblUsuario> TblUsuarios { get; set; } = new List<TblUsuario>();
}
