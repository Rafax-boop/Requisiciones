using Inventario.BLL.DTO;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRequiForm
    {
        public int? IdProvedor { get; set; }
        public string? NumRequisicion { get; set; }
        public DateTime? FechaEmision { get; set; }
        public int? IdDepartamento { get; set; }
        public string? NomResponsableDepartamento { get; set; }
        public string? Domicilio { get; set; }
        public string? Telefono { get; set; }
        public string? LugarEntrega { get; set; }
        public string? UsoEspecifico { get; set; }
        public string? Justificacion { get; set; }
        public int? IdPeriodo { get; set; }
        public bool? CuentaProgramaPresupuestario { get; set; }
        public int? IdPrioridad { get; set; }
        public int? IdUsuario { get; set; }
        public int? IdEstatus { get; set; }
        public bool? Activo { get; set; }
        public DateTime? FechaSistema { get; set; }
        public List<ItemRequiVM> Articulos { get; set; } = new();
    }

    public class ItemRequiVM
    {
        public int IdRequisicion { get; set; }
        public int? Cog { get; set; }
        public int? IdArticulo { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }
}
