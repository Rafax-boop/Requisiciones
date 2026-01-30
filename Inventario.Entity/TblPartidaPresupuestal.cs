using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblPartidaPresupuestal
{
    public int IdPartida { get; set; }

    public int? IdDepartamento { get; set; }

    public int? IdFuenteFinanciamiento { get; set; }

    public string? Cog { get; set; }

    public decimal? Disponible { get; set; }

    public bool? Activo { get; set; }
}
