using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblInventario
{
    public int Id { get; set; }

    public string Clave { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public string Descripcion { get; set; } = null!;

    public int Entrada { get; set; }

    public int Existencia { get; set; }

    public decimal Costo { get; set; }

    public decimal Iva { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal Total { get; set; }
}
