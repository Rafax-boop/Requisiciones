using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblProgramaPresupuestario
{
    public int Id { get; set; }

    public string? Pp { get; set; }

    public string? Componente { get; set; }

    public string? Actividad { get; set; }

    public string? DescripcionActividad { get; set; }

    public string? Area { get; set; }

    public string? Departamento { get; set; }

    public string? ProgramaSocial { get; set; }

    public string? UnidadMedida { get; set; }

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
