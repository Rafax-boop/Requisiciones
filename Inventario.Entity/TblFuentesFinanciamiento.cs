using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblFuentesFinanciamiento
{
    public int Id { get; set; }

    public string? Clave { get; set; }

    public string? FuenteFinanciamiento { get; set; }

    public string? Tipo { get; set; }

    public string? Etiquetado { get; set; }
}
