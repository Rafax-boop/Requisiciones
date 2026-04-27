using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblMunicipio
{
    public int IdMunicipio { get; set; }

    public string? ClaveRegion { get; set; }

    public string? NombreRegion { get; set; }

    public int? ClaveMunicipio { get; set; }

    public string? NombreMunicipios { get; set; }

    public virtual ICollection<TblRequisicionDetalleMunicipio> TblRequisicionDetalleMunicipios { get; set; } = new List<TblRequisicionDetalleMunicipio>();

    public virtual ICollection<TblRequisicion> TblRequisicions { get; set; } = new List<TblRequisicion>();
}
