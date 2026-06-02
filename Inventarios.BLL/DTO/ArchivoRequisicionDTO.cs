namespace Inventario.BLL.DTO
{
    public class ArchivoRequisicionDTO
    {
        public int Id { get; set; }
        public string? Tipo { get; set; }
        public string? Ruta { get; set; }
        public DateTime? FechaSubida { get; set; }
    }
}
