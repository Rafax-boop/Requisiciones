using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize]
    public class ConsolidadaController : Controller
    {
        private readonly IConsolidadaService _consolidadaService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IRequisicionesService _requisicionesService;

        public ConsolidadaController(IConsolidadaService consolidadaService, IWebHostEnvironment webHostEnvironment, IRequisicionesService requisicionesService)
        {
            _consolidadaService = consolidadaService;
            _webHostEnvironment = webHostEnvironment;
            _requisicionesService = requisicionesService;
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRequisicionesConsolidables()
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lista = await _consolidadaService.ObtenerRequisicionesConsolidables(idUsuario);
            return Ok(lista);
        }

        [HttpPost]
        public async Task<IActionResult> CrearConsolidada([FromBody] CrearConsolidadaRequest modelo, bool servicio)
        {
            if (modelo.IdsRequisiciones == null || modelo.IdsRequisiciones.Count < 2)
                return BadRequest(new { mensaje = "Se requieren al menos 2 requisiciones." });

            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var resultado = await _consolidadaService.CrearConsolidada(modelo.IdsRequisiciones, idUsuario, servicio);

            if (resultado == null)
                return BadRequest(new { mensaje = "No se pudo crear la consolidada. Verifica que las requisiciones no estén ya consolidadas." });

            return Ok(new { folioConsolidada = resultado.FolioConsolidada });
        }

        [HttpGet]
        public async Task<IActionResult> ListarConsolidadas(bool servicio)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            // Rol 3 o 7= analista: ve solo las asignadas a él
            // Cualquier otro rol con acceso (ej. admin): ve todas
            List<ConsolidadaDTO> lista;

            if (User.IsInRole("3") || User.IsInRole("7"))
                lista = await _consolidadaService.ListarConsolidadas(servicio, idUsuario);
            else
                lista = await _consolidadaService.ListarConsolidadas(servicio);

            lista = lista.Where(c => c.IdEstatus != 7).ToList();

            return Ok(lista);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDetalleConsolidada(int idConsolidada)
        {
            var detalle = await _consolidadaService.ObtenerDetalleConsolidada(idConsolidada);
            if (detalle == null)
                return NotFound(new { mensaje = "Consolidada no encontrada." });

            return Ok(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> AtenderConsolidada([FromBody] AtenderConsolidadaDTO modelo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _consolidadaService.AtenderConsolidada(modelo, idUsuario);
            if (!ok) return BadRequest();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SubirArchivosAtencionConsolidada(
            int IdConsolidada,
            List<IFormFile>? CuadroComparativo,
            List<IFormFile>? Anexos)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await _consolidadaService.GuardarArchivosAtencionConsolidada(
                IdConsolidada,
                CuadroComparativo ?? new List<IFormFile>(),
                Anexos ?? new List<IFormFile>(),
                webRootPath);
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPartidasConsolidada(int idConsolidada)
        {
            var partidas = await _consolidadaService.ObtenerPartidasConsolidada(idConsolidada);
            return Ok(partidas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerConsolidadasVerificadas(bool servicio)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lista = await _consolidadaService.ObtenerConsolidadasVerificadas(idUsuario, servicio);
            return Ok(lista);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerConsolidadasAutorizadas(bool servicio)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            List<ConsolidadaDTO> lista;
            if (User.IsInRole("3") || User.IsInRole("7"))
                lista = await _consolidadaService.ListarConsolidadas(servicio, idUsuario);
            else
                lista = await _consolidadaService.ListarConsolidadas(servicio);

            var estatusAutorizados = new[] { 7, 12 };
            var filtradas = lista.Where(c => estatusAutorizados.Contains(c.IdEstatus)).ToList();

            return Ok(filtradas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerExpedienteConsolidada(int idConsolidada)
        {
            var expediente = await _consolidadaService.ObtenerExpedienteConsolidada(idConsolidada);
            if (expediente == null)
                return NotFound(new { mensaje = "Consolidada no encontrada." });
            return Ok(expediente);
        }

        [HttpPost]
        public async Task<IActionResult> SubirDocumentoProveedorConsolidada(
            int idConsolidada,
            string tipoDocumento,
            IFormFile archivo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var ok = await _consolidadaService.SubirDocumentoProveedorConsolidada(
                idConsolidada, tipoDocumento, archivo, webRootPath, idUsuario);
            if (!ok) return BadRequest();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDocumentosProveedorConsolidada(int idConsolidada)
        {
            var docs = await _consolidadaService.ObtenerDocumentosProveedorConsolidada(idConsolidada);
            return Ok(docs);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerArchivosConsolidada(int idConsolidada)
        {
            var archivos = await _consolidadaService.ObtenerArchivosConsolidada(idConsolidada);
            var resultado = archivos.Select(a => new
            {
                id = a.Id,
                tipo = a.Tipo,
                ruta = a.Ruta,
                fechaSubida = a.FechaSubida?.ToString("dd/MM/yyyy HH:mm") ?? "",
                nombreArchivo = System.IO.Path.GetFileName(a.Ruta)
            }).ToList();
            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarArchivosConsolidadaZip(int idConsolidada)
        {
            var archivos = await _consolidadaService.ObtenerArchivosConsolidada(idConsolidada);
            if (archivos == null || archivos.Count == 0)
                return NotFound("No hay archivos para descargar.");

            var consolidada = await _consolidadaService.ObtenerConsolidada(idConsolidada);
            if (consolidada == null)
                return NotFound("No se encontró la consolidada.");

            using var zipStream = new MemoryStream();
            using (var zip = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                var nombresUsados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var archivo in archivos)
                {
                    if (string.IsNullOrWhiteSpace(archivo.Ruta))
                        continue;

                    var rutaRelativa = archivo.Ruta
                        .TrimStart('~', '/')
                        .Replace('/', System.IO.Path.DirectorySeparatorChar);
                    var rutaFisica = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, rutaRelativa);

                    if (!System.IO.File.Exists(rutaFisica))
                        continue;

                    var nombreOriginal = System.IO.Path.GetFileName(archivo.Ruta);
                    var nombreEntrada = ObtenerNombreZipDisponible(nombreOriginal, nombresUsados);
                    var entrada = zip.CreateEntry(nombreEntrada, CompressionLevel.Fastest);

                    await using var entryStream = entrada.Open();
                    await using var fileStream = new FileStream(rutaFisica, FileMode.Open, FileAccess.Read, FileShare.Read);
                    await fileStream.CopyToAsync(entryStream);
                }
            }

            if (zipStream.Length == 0)
                return NotFound("No se encontraron archivos físicos para descargar.");

            zipStream.Position = 0;
            var nombreZip = "Consolidada_" + (consolidada.FolioConsolidada ?? idConsolidada.ToString()) + ".zip";
            return File(zipStream.ToArray(), "application/zip", nombreZip);
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarConsolidada([FromBody] FinalizarConsolidadaRequest modelo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _consolidadaService.FinalizarConsolidada(modelo.IdConsolidada, idUsuario, modelo.Observaciones);
            return Ok(new { success = ok });
        }

        private static string ObtenerNombreZipDisponible(string nombreOriginal, HashSet<string> nombresUsados)
        {
            var baseNombre = string.IsNullOrWhiteSpace(nombreOriginal)
                ? "archivo"
                : System.IO.Path.GetFileNameWithoutExtension(nombreOriginal);
            var extension = System.IO.Path.GetExtension(nombreOriginal);
            var candidato = $"{baseNombre}{extension}";
            var indice = 2;

            while (!nombresUsados.Add(candidato))
            {
                candidato = $"{baseNombre}_{indice}{extension}";
                indice++;
            }

            return candidato;
        }
    }
}
