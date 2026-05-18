using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblProvedor
{
    public int IdProvedor { get; set; }

    public string NombreProvedor { get; set; } = null!;

    public string Rfc { get; set; } = null!;

    public string? Direccion { get; set; }

    public int? Telefono { get; set; }

    public string? Correo { get; set; }

    public string? Representante { get; set; }

    public bool Activo { get; set; }

    public virtual ICollection<TblCotizacione> TblCotizaciones { get; set; } = new List<TblCotizacione>();
}
