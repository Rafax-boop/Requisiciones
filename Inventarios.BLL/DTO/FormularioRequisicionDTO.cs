using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.DTO
{
    public class FormularioRequisicionDTO
    {
        public int? IdProvedor { get; set; }
        public string? NumRequisicion { get; set; }
        public DateTime? FechaEmision { get; set; }
        public int? IdDepartamento { get; set; }
        public string? Departamento { get; set; }
        public string? NomResponsableDepartamento { get; set; }
        public string? NomDirector { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? LugarEntrega { get; set; }
        public string? UsoEspecifico { get; set; }
        public string? Justificacion { get; set; }
        public bool? CuentaProgramaPresupuestario { get; set; }
        public int? IdUsuario { get; set; }
        public bool? Activo { get; set; }
        public DateTime? FechaSistema { get; set; }
        public List<FormularioRequisicionDetalleDTO> Articulos { get; set; } = new();
    }

    public class FormularioRequisicionDetalleDTO
    {
        public int IdRequisicion { get; set; }
        public int? Cog { get; set; }
        public int? IdArticulo { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
        public string? DescripcionDetallada { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
