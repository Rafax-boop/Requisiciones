using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize]
    [Route("DAF")]
    public class DAFController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IRequisicionesService _requisicionesService;
        private readonly IConsolidadaService _consolidadaService;
        private readonly IAlmacenService _almacenService;
        private readonly IDafService _dafService;

        public DAFController(
            IMapper mapper,
            IRequisicionesService requisicionesService,
            IConsolidadaService consolidadaService,
            IAlmacenService almacenService,
            IDafService dafService)
        {
            _mapper = mapper;
            _requisicionesService = requisicionesService;
            _consolidadaService = consolidadaService;
            _almacenService = almacenService;
            _dafService = dafService;
        }

        [HttpGet("")]
        [HttpGet("TablaDAF")]
        public async Task<IActionResult> TablaDAF()
        {
            ViewData["Title"] = "DAF";

            var consolidadasMateriales = await _consolidadaService.ListarConsolidadas(false);
            var consolidadasServicios = await _consolidadaService.ListarConsolidadas(true);

            var consolidadas = consolidadasMateriales
                .Concat(consolidadasServicios)
                .Where(c => c.IdEstatus == 16)
                .OrderBy(c => c.FechaCreacion)
                .ThenBy(c => c.ConsolidadaID)
                .ToList();

            var vm = new VMTablaRequisiciones
            {
                Requisiciones = new List<VMRequisicionMaestra>(),
                Consolidadas = consolidadas,
                Estatus = await _almacenService.ObtenerEstatus()
            };

            return View(vm);
        }

        [HttpGet("ObtenerProgresoConsolidada")]
        public async Task<IActionResult> ObtenerProgresoConsolidada(int idConsolidada)
        {
            if (idConsolidada <= 0)
                return Json(new List<ProgresoPasoDTO>());

            try
            {
                var pasos = await _consolidadaService.ObtenerProgresoConsolidada(idConsolidada);
                return Json(pasos);
            }
            catch (Exception ex)
            {
                var baseEx = ex.GetBaseException();
                return new JsonResult(new { message = $"No se pudo cargar el historial de la consolidada: {baseEx.Message}" })
                {
                    StatusCode = 500
                };
            }
        }

        [HttpGet("HistorialConsolidadaParaPdf")]
        public async Task<IActionResult> HistorialConsolidadaParaPdf(int idConsolidada)
        {
            try
            {
                var dto = await _consolidadaService.ObtenerDetalleConsolidada(idConsolidada);
                var consolidada = await _consolidadaService.ObtenerConsolidada(idConsolidada);
                if (dto == null || consolidada == null)
                    return NotFound();

                var historial = await _consolidadaService.ObtenerProgresoConsolidada(idConsolidada);
                var fechaEmision = DateOnly.FromDateTime(consolidada.FechaCreacion);

                var departamentos = string.Join(", ", (dto.Requisiciones ?? new List<RequiHijaDTO>())
                    .Select(r => r.Departamento)
                    .Where(d => !string.IsNullOrWhiteSpace(d))
                    .Distinct());

                var vm = new VMHistorialRequisicionPdf
                {
                    IdRequisicion = idConsolidada,
                    TipoRequisicion = consolidada.RequiServicio == true ? "Consolidada de Servicios" : "Consolidada de Materiales",
                    Folio = dto.FolioConsolidada ?? consolidada.FolioConsolidada,
                    FechaEmision = fechaEmision,
                    Departamento = string.IsNullOrWhiteSpace(departamentos) ? "Sin departamento" : departamentos,
                    Responsable = string.IsNullOrWhiteSpace(dto.CreadoPor) ? "Sin responsable" : dto.CreadoPor,
                    IdEstatus = dto.IdEstatus ?? consolidada.IdEstatus,
                    EstatusActual = string.IsNullOrWhiteSpace(dto.Estatus) ? "Sin estatus" : dto.Estatus,
                    Historial = historial.Select(p => new VMHistorialPasoPdf
                    {
                        Departamento = p.Dept ?? "",
                        Fecha = p.Date ?? "",
                        Hora = p.Time ?? "",
                        Estado = p.State ?? "",
                        Responsable = p.By ?? "",
                        Accion = p.Action ?? "",
                        Nota = p.Comment ?? ""
                    }).ToList()
                };

                return View("~/Views/Shared/HistorialRequisicionParaPdf.cshtml", vm);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error al generar el historial de la consolidada: {ex.GetBaseException().Message}" });
            }
        }

        [HttpPost("AutorizarRequisicion")]
        public async Task<IActionResult> AutorizarRequisicion(int idRequi, string? observaciones)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.AutorizarRequisicion(idRequi, idUsuario, observaciones);
            return Json(new { success = ok });
        }

        [HttpPost("AutorizarConsolidada")]
        public async Task<IActionResult> AutorizarConsolidada(int idConsolidada, string? observaciones)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.AutorizarConsolidada(idConsolidada, idUsuario, observaciones);
            return Json(new { success = ok });
        }

        [HttpPost("SolicitarModificacionRequisicion")]
        public async Task<IActionResult> SolicitarModificacionRequisicion(int idRequi, string observaciones)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.SolicitarModificacionRequisicion(idRequi, idUsuario, observaciones);
            return Json(new { success = ok });
        }

        [HttpPost("SolicitarModificacionConsolidada")]
        public async Task<IActionResult> SolicitarModificacionConsolidada(int idConsolidada, string observaciones)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.SolicitarModificacionConsolidada(idConsolidada, idUsuario, observaciones);
            return Json(new { success = ok });
        }

        [HttpPost("RechazarRequisicion")]
        public async Task<IActionResult> RechazarRequisicion(int idRequi, string motivo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.RechazarRequisicion(idRequi, idUsuario, motivo);
            return Json(new { success = ok });
        }

        [HttpPost("RechazarConsolidada")]
        public async Task<IActionResult> RechazarConsolidada(int idConsolidada, string motivo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _dafService.RechazarConsolidada(idConsolidada, idUsuario, motivo);
            return Json(new { success = ok });
        }
    }
}
