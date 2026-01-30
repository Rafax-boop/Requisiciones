using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblProvedor
{
    public int IdProvedor { get; set; }

    public int IdProvedorTipo { get; set; }

    public string NombreProvedor { get; set; } = null!;

    public string Direccion { get; set; } = null!;

    public string Rfc { get; set; } = null!;

    public DateTime FechaSistema { get; set; }

    public decimal Presupuesto { get; set; }

    public bool Activo { get; set; }

    public DateTime? FechaBaja { get; set; }

    public virtual TblProvedorTipo IdProvedorTipoNavigation { get; set; } = null!;
}
