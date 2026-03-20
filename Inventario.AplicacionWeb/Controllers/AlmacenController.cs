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

        [HttpPost]
        public async Task<IActionResult> RegistrarIngreso([FromBody] IngresoInventarioRequest request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var (ok, error) = await _almacenService.RegistrarIngresoInventario(
                request.Clave,
                request.Descripcion ?? "",
                request.UnidadMedida ?? "",
                request.Cantidad,
                idUsuario,
                request.Motivo ?? "");

            return Json(new { ok, error });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarCompleta([FromBody] AprobarRequest request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var (ok, error) = await _almacenService.AprobarRequisicionCompleta(request.IdRequisicion, idUsuario);
            return Json(new { ok, error });
        }

        [HttpPost]
        public async Task<IActionResult> AprobarParcial([FromBody] AprobarParcialRequest request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var partidas = (request.Partidas ?? new List<AprobarParcialPartidaRequest>())
                .Select(p => (p.IdRequisicionDetalle, p.CantidadAprobada));

            var (ok, error) = await _almacenService.AprobarRequisicionParcial(request.IdRequisicion, idUsuario, partidas);
            return Json(new { ok, error });
        }

        [HttpPost]
        public async Task<IActionResult> Rechazar([FromBody] RechazarRequest request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var (ok, error) = await _almacenService.RechazarRequisicionAlmacen(request.IdRequisicion, idUsuario, request.Motivo ?? "");
            return Json(new { ok, error });
        }

        [HttpPost]
        public async Task<IActionResult> AnularMovimiento([FromBody] AnularMovimientoRequest request)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return Unauthorized(new { ok = false, error = "No autorizado." });

            var (ok, error) = await _almacenService.AnularMovimientoInventario(request.IdMovimiento, idUsuario, request.Motivo ?? "");
            return Json(new { ok, error });
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
    }
}
