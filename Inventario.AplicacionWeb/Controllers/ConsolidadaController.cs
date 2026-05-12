using Inventario.AplicacionWeb.Models.ViewModels;
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
        public async Task<IActionResult> CrearConsolidada([FromBody] CrearConsolidadaRequest modelo)
        {
            if (modelo.IdsRequisiciones == null || modelo.IdsRequisiciones.Count < 2)
                return BadRequest(new { mensaje = "Se requieren al menos 2 requisiciones." });

            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var resultado = await _consolidadaService.CrearConsolidada(modelo.IdsRequisiciones, idUsuario);

            if (resultado == null)
                return BadRequest(new { mensaje = "No se pudo crear la consolidada. Verifica que las requisiciones no estén ya consolidadas." });

            return Ok(new { folioConsolidada = resultado.FolioConsolidada });
        }
    }
}
