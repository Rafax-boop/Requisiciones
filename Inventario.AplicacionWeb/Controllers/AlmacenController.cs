using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize(Roles = "2,4,5")]
    public class AlmacenController : Controller
    {
        private readonly IRequisicionesService _requisicionService;
        private readonly IAlmacenService _almacenService;
        private readonly IMapper _mapper;

        public AlmacenController(
            IRequisicionesService requisicionService,
            IAlmacenService almacenService,
            IMapper mapper)
        {
            _requisicionService = requisicionService;
            _almacenService = almacenService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var requisicionesDto = await _almacenService.ListarRequisicionesAlmacen();
            var requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(requisicionesDto);

            var inventarioDto = await _almacenService.ObtenerInventario();
            var inventario = _mapper.Map<List<VMInventarioItem>>(inventarioDto);

            var estatus = await _almacenService.ObtenerEstatus();

            var unidadesMedida = inventario
                .Select(x => x.UnidadMedida)
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            var vm = new VMAlmacenIndex
            {
                Requisiciones = requisiciones,
                Inventario = inventario,
                Estatus = estatus,
                UnidadesMedida = unidadesMedida
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRequisicionCompleta(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();
            return Json(dto);
        }

        [HttpGet]
        public async Task<IActionResult> ConsultarStock(int id)
        {
            var stock = await _almacenService.ConsultarStockParaRequisicion(id);
            return Json(stock);
        }

        private int? GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out int id))
                return id;
            return null;
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarIngreso([FromBody] IngresoInventarioRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            try
            {
                await _almacenService.RegistrarIngresoInventario(new IngresoInventarioDTO
                {
                    Clave = request.Clave,
                    Descripcion = request.Descripcion,
                    UnidadMedida = request.UnidadMedida,
                    Cantidad = request.Cantidad,
                    Motivo = request.Motivo
                });
                return Json(new { ok = true, mensaje = "Ingreso registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AprobarCompleta([FromBody] JsonElement body)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            if (!body.TryGetProperty("idRequisicion", out var prop) || !prop.TryGetInt32(out int idRequisicion))
                return BadRequest(new { ok = false, error = "idRequisicion inválido." });

            try
            {
                await _almacenService.AprobarRequisicionCompleta(idRequisicion, userId.Value);
                return Json(new { ok = true, mensaje = "Requisición autorizada completa." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> AprobarParcial([FromBody] AprobarParcialRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var partidas = (request.Partidas ?? new List<AprobarParcialPartidaRequest>())
                .Select(p => (p.IdRequisicionDetalle, p.CantidadAprobada));

            try
            {
                await _almacenService.AprobarRequisicionParcial(request.IdRequisicion, userId.Value, partidas);
                return Json(new { ok = true, mensaje = "Requisición autorizada parcialmente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar([FromBody] RechazarRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            try
            {
                await _almacenService.RechazarRequisicionAlmacen(
                    request.IdRequisicion, userId.Value, request.Motivo ?? "");
                return Json(new { ok = true, mensaje = "Requisición rechazada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarRequisicion([FromBody] ProcesarRequisicionRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var entregas = (request.Entregas ?? new List<PartidaEntregaRequest>())
                .Select(e => (e.IdRequisicionDetalle, e.CantidadAprobada));

            var compras = (request.Compras ?? new List<PartidaCompraRequest>())
                .Select(c => (c.IdRequisicionDetalle, c.CantidadComprar));

            try
            {
                await _almacenService.ProcesarRequisicion(
                    request.IdRequisicion, userId.Value, entregas, compras);
                return Json(new { ok = true, mensaje = "Requisición procesada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}
