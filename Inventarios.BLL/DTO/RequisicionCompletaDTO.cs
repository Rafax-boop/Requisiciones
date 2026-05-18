using System;
using System.Collections.Generic;

namespace Inventario.BLL.DTO
{
    public class RequisicionCompletaDTO
    {
        public string? NumRequisicion { get; set; }
        public string? NumPedido { get; set; }
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
        public int? IdAdquisicion { get; set; }
        public bool? UsoMaterial { get; set; }
        public string? TipoServicio { get; set; }
        public DateOnly? FechaServicio { get; set; }
        public bool? RequiServicio { get; set; }
        public int IdEstatus { get; set; }
        public string? Estatus { get; set; }
        public string? Hash { get; set; }
        public List<DetalleArticuloDTO> Articulos { get; set; } = new();
    }
}
