using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblArticulo
{
    public int Id { get; set; }

    public int Cog { get; set; }

    public int ClaveDivision { get; set; }

    public int ClaveMaterial { get; set; }

    public string Descripcion { get; set; } = null!;

    public string UnidadMedida { get; set; } = null!;

    public string? Marca { get; set; }

    public string? Modelo { get; set; }

    public string? Tipo { get; set; }

    public int? Ano { get; set; }

    public int? Maximo { get; set; }

    public int? Estatus { get; set; }
}
