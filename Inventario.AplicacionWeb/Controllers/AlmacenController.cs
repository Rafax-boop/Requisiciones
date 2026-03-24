using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var (ok, mensaje, error) = await _almacenService.RegistrarIngresoInventario(
                request.Clave,
                request.Descripcion ?? "",
                request.UnidadMedida ?? "",
                request.Cantidad,
                userId.Value,
                request.Motivo ?? "");

            return Json(new { ok, mensaje, error });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarCompleta([FromBody] AprobarRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var (ok, mensaje, error) = await _almacenService.AprobarRequisicionCompleta(request.IdRequisicion, userId.Value);
            return Json(new { ok, mensaje, error });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarParcial([FromBody] AprobarParcialRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var partidas = (request.Partidas ?? new List<AprobarParcialPartidaRequest>())
                .Select(p => (p.IdRequisicionDetalle, p.CantidadAprobada));

            var (ok, mensaje, error) = await _almacenService.AprobarRequisicionParcial(request.IdRequisicion, userId.Value, partidas);
            return Json(new { ok, mensaje, error });
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar([FromBody] RechazarRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var (ok, mensaje, error) = await _almacenService.RechazarRequisicionAlmacen(request.IdRequisicion, userId.Value, request.Motivo ?? "");
            return Json(new { ok, mensaje, error });
        }

        [HttpPost]
        public async Task<IActionResult> AnularMovimiento([FromBody] AnularMovimientoRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var (ok, mensaje, error) = await _almacenService.AnularMovimientoInventario(request.IdMovimiento, userId.Value, request.Motivo ?? "");
            return Json(new { ok, mensaje, error });
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarRequisicion([FromBody] ProcesarRequisicionRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, mensaje = (string?)null, error = "No autorizado." });

            var entregas = (request.Entregas ?? new List<PartidaEntrega>())
                .Select(e => (e.IdRequisicionDetalle, e.CantidadAprobada));

            var comprasParam = (request.Compras ?? new List<PartidaCompra>())
                .Select(c => (c.IdRequisicionDetalle, c.CantidadComprar));

            var (ok, mensaje, error) = await _almacenService.ProcesarRequisicion(
                request.IdRequisicion, userId.Value, entregas, comprasParam);
            return Json(new { ok, mensaje, error });
        }

        public class IngresoInventarioRequest
        {
            public string? Clave { get; set; }
            public string? Descripcion { get; set; }
            public string? UnidadMedida { get; set; }
            public int Cantidad { get; set; }
            public string? Motivo { get; set; }
        }

        public class AprobarRequest
        {
            public int IdRequisicion { get; set; }
        }

        public class AprobarParcialRequest
        {
            public int IdRequisicion { get; set; }
            public List<AprobarParcialPartidaRequest>? Partidas { get; set; }
        }

        public class AprobarParcialPartidaRequest
        {
            public int IdRequisicionDetalle { get; set; }
            public int CantidadAprobada { get; set; }
        }

        public class RechazarRequest
        {
            public int IdRequisicion { get; set; }
            public string? Motivo { get; set; }
        }

        public class AnularMovimientoRequest
        {
            public int IdMovimiento { get; set; }
            public string? Motivo { get; set; }
        }

        public class ProcesarRequisicionRequest
        {
            public int IdRequisicion { get; set; }
            public List<PartidaEntrega>? Entregas { get; set; }
            public List<PartidaCompra>? Compras { get; set; }
        }

        public class PartidaEntrega
        {
            public int IdRequisicionDetalle { get; set; }
            public int CantidadAprobada { get; set; }
        }

        public class PartidaCompra
        {
            public int IdRequisicionDetalle { get; set; }
            public int CantidadComprar { get; set; }
        }
    }
}
