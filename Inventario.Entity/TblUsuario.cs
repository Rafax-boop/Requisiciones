using System;
using System.Collections.Generic;

namespace Inventario.Entity;

public partial class TblUsuario
{
    public int IdUsuario { get; set; }

    public string? Usuario { get; set; }

    public string? Pasword { get; set; }

    public string? Telefono { get; set; }

    public string? Correo { get; set; }

    public int? IdDepartamento { get; set; }

    public int? IdRol { get; set; }

    public bool? Activo { get; set; }

    public virtual TblDepartamento? IdDepartamentoNavigation { get; set; }

    public virtual TblRol? IdRolNavigation { get; set; }

    public virtual ICollection<TblBitacoraEstatus> TblBitacoraEstatuses { get; set; } = new List<TblBitacoraEstatus>();

    public virtual ICollection<TblRequisicion> TblRequisicionIdUsuarioMatNavigations { get; set; } = new List<TblRequisicion>();

    public virtual ICollection<TblRequisicion> TblRequisicionIdUsuarioNavigations { get; set; } = new List<TblRequisicion>();
}
