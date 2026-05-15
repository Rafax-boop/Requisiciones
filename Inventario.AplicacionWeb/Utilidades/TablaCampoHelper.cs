using System.Net;

namespace Inventario.AplicacionWeb.Utilidades
{
    public static class TablaCampoHelper
    {
        public static string ObtenerFolio(string? folio)
        {
            var texto = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(folio) ? "—" : folio);
            return $"<span class=\"folio-badge\">{texto}</span>";
        }

        public static string ObtenerCampoConIcono(string? texto, string icono, string? secundaria = null)
        {
            var principal = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(texto) ? "—" : texto);
            var detalle = string.IsNullOrWhiteSpace(secundaria)
                ? string.Empty
                : $"<span class=\"tabla-meta-secundaria\">{WebUtility.HtmlEncode(secundaria)}</span>";

            return
                "<div class=\"tabla-meta-stack\">" +
                $"<span class=\"tabla-meta-principal\"><i class=\"{icono}\"></i>{principal}</span>" +
                detalle +
                "</div>";
        }
    }
}
