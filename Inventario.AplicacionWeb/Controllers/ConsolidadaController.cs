using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize]
    public class ConsolidadaController : Controller
    {
        private readonly IConsolidadaService _consolidadaService;

        public ConsolidadaController(IConsolidadaService consolidadaService)
        {
            _consolidadaService = consolidadaService;
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
            var resultado = await _consolidadaService.CrearConsolidada(modelo.IdsRequisiciones, idUsuario, false);

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
    }
}
