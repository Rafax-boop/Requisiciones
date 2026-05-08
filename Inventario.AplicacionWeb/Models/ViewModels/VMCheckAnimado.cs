namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMCheckAnimado
    {
        public string Type { get; set; } = "checkbox";
        public string? Name { get; set; }
        public string? Value { get; set; }
        public string? Id { get; set; }
        public string? Title { get; set; }
        public string? Text { get; set; }
        public string? ClassName { get; set; }
        public string? InputClass { get; set; }
        public bool Checked { get; set; }
        public bool Required { get; set; }
    }
}
