using System;

namespace Inventario.Entity;

public partial class TblMovimientoInventario
{
    public int Id { get; set; }

    public int IdInventario { get; set; }

    /// <summary>
    /// I = Ingreso, E = Egreso, A = Ajuste
    /// </summary>
    public string TipoMovimiento { get; set; } = null!;

    public int Cantidad { get; set; }

    public DateTime Fecha { get; set; }

    public string Motivo { get; set; } = null!;

    public int? IdRequisicion { get; set; }

    public int? IdRequisicionDetalle { get; set; }

    public int IdUsuario { get; set; }

    public bool Anulado { get; set; }

    public string? MotivoAnulacion { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public int? IdUsuarioAnula { get; set; }

    public virtual TblInventario IdInventarioNavigation { get; set; } = null!;
}

