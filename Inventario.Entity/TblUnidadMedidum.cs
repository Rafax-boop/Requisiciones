using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblUnidadMedidum
{
    public int IdUnidadMedida { get; set; }

    public string? DescripcionUnidadMedida { get; set; }

    public bool? Activo { get; set; }
}
