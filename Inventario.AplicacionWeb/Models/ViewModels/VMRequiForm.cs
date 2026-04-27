using Inventario.BLL.DTO;

namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMRequiForm
    {
        public int? IdRequiMaestra { get; set; }
        public string? ObservacionesBitacora { get; set; }
        public string? NumRequisicion { get; set; }
        public DateOnly? FechaEmision { get; set; }
        public int? IdDepartamento { get; set; }
        public string? Departamento { get; set; }
        public string? NomResponsableDepartamento { get; set; }
        public string? CargoResponsableDepartamento { get; set; }
        public string? NomDirector { get; set; }
        public string? CargoDirector { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? LugarEntrega { get; set; }
        public string? UsoEspecifico { get; set; }
        public string? Justificacion { get; set; }
        public bool? CuentaProgramaPresupuestario { get; set; }
        public bool? UsoMaterial { get; set; }
        public int? IdUsuario { get; set; }
        public string? Hash { get; set; }
        public DateTime? FechaSistema { get; set; }
        public string? TipoServicio { get; set; }
        public DateOnly? FechaServicio { get; set; }
        public bool? RequiServicio { get; set; }
        public int? NumeroFormato { get; set; }
        public List<ItemRequiVM> Articulos { get; set; } = new();
        public List<VMFotoExistente> FotosExistentes { get; set; } = new();
    }

    public class ItemRequiVM
    {
        public int IdRequisicion { get; set; }
        public int? Cog { get; set; }
        public int? IdArticulo { get; set; }
        public string? ClaveMaterial { get; set; }
        public decimal? Cantidad { get; set; }
        public string? UnidadMedida { get; set; }
        public string? Descripcion { get; set; }
        public string? DescripcionDetallada { get; set; }
        public DateTime? FechaRegistro { get; set; }

        public int? IdMunicipio { get; set; }

        public string? TipoProgramacion { get; set; }
        public int? Llenado1 { get; set; }
        public int? Llenado2 { get; set; }
        public int? Llenado3 { get; set; }
        public int? Llenado4 { get; set; }
        public int? Llenado5 { get; set; }
        public int? Llenado6 { get; set; }
        public int? Llenado7 { get; set; }
        public int? Llenado8 { get; set; }
        public int? Llenado9 { get; set; }
        public int? Llenado10 { get; set; }
        public int? Llenado11 { get; set; }
        public int? Llenado12 { get; set; }
        public int? Mes { get; set; }

        public List<MunicipioItemDTO>? Municipios { get; set; }
    }

    public class VMFotoExistente
    {
        public int IdFoto { get; set; }
        public string Ruta { get; set; }
    }
}
