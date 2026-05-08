using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblProveedorGanador
{
    public int IdGanador { get; set; }

    public int IdRequisicion { get; set; }

    public int IdProveedor { get; set; }

    public decimal? Subtotal { get; set; }

    public decimal? Iva { get; set; }

    public decimal? Total { get; set; }

    public bool SeleccionManual { get; set; }

    public string? Justificacion { get; set; }

    public int? IdUsuario { get; set; }

    public DateTime? FechaSeleccion { get; set; }
}
