namespace Inventario.AplicacionWeb.Models.ViewModels
{
    /// <summary>
    /// Item de inventario para la pestaña Inventario en Almacén (desde TblInventario).
    /// </summary>
    public class VMInventarioItem
    {
        public string Descripcion { get; set; } = "";
        public string UnidadMedida { get; set; } = "";
        public int Existencia { get; set; }
        /// <summary>
        /// Stock mínimo (0 si no existe en BD; TblInventario no tiene este campo).
        /// </summary>
        public int Minimo { get; set; }
        /// <summary>
        /// "OK" | "Stock bajo" | "Sin stock"
        /// </summary>
        public string Situacion { get; set; } = "";
    }
}
