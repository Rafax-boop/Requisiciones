using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
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
        private readonly IWebHostEnvironment _webHostEnvironment;

        public AlmacenController(
            IRequisicionesService requisicionService,
            IAlmacenService almacenService,
            IMapper mapper,
            IWebHostEnvironment webHostEnvironment)
        {
            _requisicionService = requisicionService;
            _almacenService = almacenService;
            _mapper = mapper;
            _webHostEnvironment = webHostEnvironment;
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

            var entregasPendientes = await _almacenService.ListarEntregasPendientes();

            var vm = new VMAlmacenIndex
            {
                Requisiciones = requisiciones,
                Inventario = inventario,
                Estatus = estatus,
                UnidadesMedida = unidadesMedida,
                EntregasPendientes = entregasPendientes
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
        public async Task<IActionResult> VerSalidaParaPdf(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var entregasPendientes = await _almacenService.ListarEntregasPendientes();
            var entregaActual = entregasPendientes.FirstOrDefault(x => x.IdRequisicion == id);
            var articulosPendientes = entregaActual?.Articulos ?? new List<ArticuloEntregaDTO>();

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                UsoMaterial = dto.UsoMaterial,
                Articulos = articulosPendientes.Select(a => new ItemRequiVM
                {
                    ClaveMaterial = a.IdRequisicionDetalle,
                    Cantidad = a.CantidadMovimiento,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.Descripcion
                }).ToList()
            };

            return View("SalidaMaterialesParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ConsultarStock(int id)
        {
            var stock = await _almacenService.ConsultarStockParaRequisicion(id);
            return Json(stock);
        }

        /// <summary>
        /// Devuelve los artùculos pendientes de entrega fùsica para una requisiciùn.
        /// Usado por el tab "A Entregar" al abrir el modal de detalle.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ObtenerEntregasPendientes()
        {
            var data = await _almacenService.ListarEntregasPendientes();
            return Json(data);
        }

        /// <summary>
        /// Confirma la entrega fùsica de los artùculos seleccionados.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmarEntrega([FromForm] ConfirmarEntregaRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            if (request == null || request.IdsMovimientos == null || request.IdsMovimientos.Count == 0)
                return BadRequest(new { ok = false, error = "Debe seleccionar al menos un artùculo." });

            if (request.FormatoSalidaFirmado == null || request.FormatoSalidaFirmado.Length == 0)
                return BadRequest(new { ok = false, error = "Debes subir el formato de salida firmado." });

            try
            {
                var extension = Path.GetExtension(request.FormatoSalidaFirmado.FileName)?.ToLowerInvariant();
                if (extension != ".pdf")
                    return BadRequest(new { ok = false, error = "El formato firmado debe ser un archivo PDF." });

                var carpetaRelativa = Path.Combine("uploads", "formato-salida", request.IdRequisicion.ToString());
                var carpetaFisica = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa);
                Directory.CreateDirectory(carpetaFisica);

                var nombreArchivo = $"formato_firmado_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

                await using (var stream = new FileStream(rutaFisica, FileMode.Create))
                {
                    await request.FormatoSalidaFirmado.CopyToAsync(stream);
                }

                var rutaDb = "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace("\\", "/");
                await _almacenService.ConfirmarEntrega(
                    request.IdRequisicion,
                    request.IdsMovimientos,
                    userId.Value,
                    rutaDb);

                return Json(new { ok = true, mensaje = "Entrega confirmada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
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
                return BadRequest(new { ok = false, error = "idRequisicion invùlido." });

            try
            {
                await _almacenService.AprobarRequisicionCompleta(idRequisicion, userId.Value);
                return Json(new { ok = true, mensaje = "Requisiciùn autorizada completa." });
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
                return Json(new { ok = true, mensaje = "Requisiciùn autorizada parcialmente." });
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
                return Json(new { ok = true, mensaje = "Requisiciùn rechazada correctamente." });
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
                return Json(new { ok = true, mensaje = "Requisiciùn procesada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }
    }
}
