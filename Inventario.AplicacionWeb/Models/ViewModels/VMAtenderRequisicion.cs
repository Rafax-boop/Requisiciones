namespace Inventario.AplicacionWeb.Models.ViewModels
{
    public class VMAtenderRequisicion
    {
        public int IdRequisicion { get; set; }
        public string Observaciones { get; set; }
        public bool RequiereModificacion { get; set; }
        public int IdPP { get; set; }
        public string? FF { get; set; }
        public string? TipoPrograma { get; set; }
        public int ClaveRegion { get; set; }
        public List<CogEditado> CogsEditados { get; set; } = new();
    }

    public class CogEditado
    {
        public int IdArticulo { get; set; }
        public int Cog { get; set; }
    }
}
