using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblProvedorTipo
{
    public int IdProvedorTipo { get; set; }

    public string Descricpion { get; set; } = null!;

    public string? Sigla { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblProvedor> TblProvedors { get; set; } = new List<TblProvedor>();
}
