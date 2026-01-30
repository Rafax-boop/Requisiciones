using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblArticulo
{
    public int IdArticulo { get; set; }

    public short? EjercicioFiscal { get; set; }

    public string? Capitulo { get; set; }

    public string? PartidaEspecifica { get; set; }

    public string? ClaveActividadEspecifica { get; set; }

    public string? NombreActividadEspecifica { get; set; }

    public string? Articulo { get; set; }

    public string? Descripcion { get; set; }

    public string? ClaveUnidadMedida { get; set; }

    public string? DescripcionUnidadMedida { get; set; }

    public decimal? CostoPromedio { get; set; }

    public decimal? CostoEstimado { get; set; }

    public bool? Activo { get; set; }
}
