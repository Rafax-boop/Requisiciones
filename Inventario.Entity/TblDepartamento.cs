using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblDepartamento
{
    public int IdDepartamento { get; set; }

    public string NombreDepartamento { get; set; } = null!;

    public string? NombreJefe { get; set; }

    public string? CargoJefe { get; set; }

    public string? NombreDirector { get; set; }

    public string? CargoDirector { get; set; }

    public string? Telefono { get; set; }

    public string? NivelOrganigrama { get; set; }

    public bool? Activo { get; set; }

    public string? CveRf { get; set; }
}
