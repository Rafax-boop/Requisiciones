using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblRequisicion
{
    public int IdRequisicion { get; set; }

    public string? NumRequisicion { get; set; }

    public DateOnly? FechaEmision { get; set; }

    public int? IdDepartamento { get; set; }

    public string? NomResponsableDepartamento { get; set; }

    public string? NombreDirector { get; set; }

    public string? Correo { get; set; }

    public string? Telefono { get; set; }

    public string? LugarEntrega { get; set; }

    public string? UsoEspecifico { get; set; }

    public string? Justificacion { get; set; }

    public bool? CuentaProgramaPresupuestario { get; set; }

    public bool? Donativo { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdEstatus { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public int Consecutivo { get; set; }

    public int? IdUsuarioMat { get; set; }

    public int? IdUsuarioFinan { get; set; }

    public string? Hash { get; set; }

    public int? IdPp { get; set; }

    public string? Ff { get; set; }

    public string? TipoPrograma { get; set; }

    public int? ClaveRegion { get; set; }

    public string? TipoServicio { get; set; }

    public DateOnly? FechaServicio { get; set; }

    public bool? RequiServicio { get; set; }

    public string? NumApi { get; set; }

    public virtual TblMunicipio? ClaveRegionNavigation { get; set; }

    public virtual TblDepartamento? IdDepartamentoNavigation { get; set; }

    public virtual TblEstatus? IdEstatusNavigation { get; set; }

    public virtual TblProgramaPresupuestario? IdPpNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioFinanNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioMatNavigation { get; set; }

    public virtual TblUsuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<TblArticulosProgramado> TblArticulosProgramados { get; set; } = new List<TblArticulosProgramado>();

    public virtual ICollection<TblBitacoraEstatus> TblBitacoraEstatuses { get; set; } = new List<TblBitacoraEstatus>();

    public virtual ICollection<TblCotizacione> TblCotizaciones { get; set; } = new List<TblCotizacione>();

    public virtual ICollection<TblRegistroDiseno> TblRegistroDisenos { get; set; } = new List<TblRegistroDiseno>();

    public virtual ICollection<TblRequisicionDetalleMovimiento> TblRequisicionDetalleMovimientos { get; set; } = new List<TblRequisicionDetalleMovimiento>();

    public virtual ICollection<TblRequisicionDetalle> TblRequisicionDetalles { get; set; } = new List<TblRequisicionDetalle>();
}
