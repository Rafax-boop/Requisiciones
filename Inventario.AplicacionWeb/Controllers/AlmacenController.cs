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
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var entregasPendientes = await _almacenService.ListarEntregasPendientesAlmacen();

            var pedidosDto = await _almacenService.ListarPedidosAlmacen();
            var pedidos = _mapper.Map<List<VMRequisicionMaestra>>(pedidosDto);

            var reqDocumentosDto = await _almacenService.ListarRequisicionesConDocumentos();
            var reqDocumentos = _mapper.Map<List<VMRequisicionMaestra>>(reqDocumentosDto);

            var vm = new VMAlmacenIndex
            {
                Requisiciones = requisiciones,
                Inventario = inventario,
                Estatus = estatus,
                UnidadesMedida = unidadesMedida,
                EntregasPendientes = entregasPendientes,
                Pedidos = pedidos,
                RequisicionesConDocumentos = reqDocumentos
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDocumentos(int idRequisicion)
        {
            try
            {
                var documentosDto = await _almacenService.ObtenerDocumentosPorRequisicion(idRequisicion);
                var documentos = documentosDto.Select(d => new VMDocumentoExpediente
                {
                    Nombre = d.Nombre,
                    Tipo = d.Tipo,
                    Ruta = d.Ruta,
                    FechaSubida = d.FechaSubida?.ToString("dd/MM/yyyy HH:mm")
                }).ToList();

                return Json(new { ok = true, data = documentos });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        /// <summary>IDs por pestaña para notificaciones (sondeo en cliente).</summary>
        [HttpGet]
        public async Task<IActionResult> SnapshotIdsPorTab()
        {
            var requisicionesDto = await _almacenService.ListarRequisicionesAlmacen();
            var entregasDto = await _almacenService.ListarEntregasPendientesAlmacen();
            var pedidosDto = await _almacenService.ListarPedidosAlmacen();
            var expedientesDto = await _almacenService.ListarRequisicionesConDocumentos();

            var snapshot = new
            {
                requisiciones = requisicionesDto.Select(r => r.IdRequi).OrderBy(x => x).ToList(),
                entregas = entregasDto.Select(e => e.EsConsolidada ? e.ConsolidadaId ?? e.IdRequisicion : e.IdRequisicion).OrderBy(x => x).ToList(),
                pedidos = pedidosDto.Select(r => r.IdRequi).OrderBy(x => x).ToList(),
                expedientes = expedientesDto.Select(r => r.IdRequi).OrderBy(x => x).ToList()
            };

            return Json(snapshot);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerRequisicionCompleta(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();
            return Json(dto);
        }

        public async Task<IActionResult> VerSalidaParaPdf(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null) return NotFound();

            // Generar o reutilizar el folio
            var numeroFormato = await _almacenService.GenerarFormatoSalida(id, userId.Value);

            var entregasPendientes = await _almacenService.ListarEntregasPendientes();
            var entregaActual = entregasPendientes.FirstOrDefault(x => x.IdRequisicion == id);
            var articulosPendientes = entregaActual?.Articulos ?? new List<ArticuloEntregaDTO>();

            var detallePorId = dto.Articulos
                .GroupBy(x => x.IdRequisicionDetalle)
                .ToDictionary(g => g.Key, g => g.First());

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                NumPedidos = dto.NumPedidos,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                UsoMaterial = dto.UsoMaterial,
                NumeroFormato = numeroFormato,
                Articulos = articulosPendientes.Select(a =>
                {
                    detallePorId.TryGetValue(a.IdRequisicionDetalle, out var det);
                    return new ItemRequiVM
                    {
                        IdArticulo = det?.IdArticulo,
                        Cog = det?.NumPartida,
                        ClaveMaterial = det?.ClaveMaterial,
                        Cantidad = a.EsDeCompra ? a.CantidadOriginal : a.CantidadMovimiento,
                        UnidadMedida = a.UnidadMedida,
                        Descripcion = a.Descripcion,
                        DescripcionDetallada = a.Descripcion
                    };
                }).ToList()
            };

            return View("SalidaMaterialesParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> VerSalidaParaPdfConsolidada(int idConsolidada)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var consolidada = await _almacenService.ObtenerConsolidadaAsync(idConsolidada);
            if (consolidada == null) return NotFound();

            var numeroFormato = await _almacenService.GenerarFormatoSalidaConsolidada(idConsolidada, userId.Value);

            var entregasGrouped = await _almacenService.ListarEntregasPendientesAlmacen();
            var entregaActual = entregasGrouped.FirstOrDefault(x => x.ConsolidadaId == idConsolidada);
            var articulosPendientes = entregaActual?.Articulos ?? new List<ArticuloEntregaDTO>();

            var depto19 = await _almacenService.ObtenerDepartamentoRecursosMateriales();

            var childQuery = await _almacenService.ObtenerChildRequisicionData(idConsolidada);

            var vm = new VMRequiForm
            {
                IdRequiMaestra = null,
                NumRequisicion = consolidada.FolioConsolidada,
                NumPedidos = consolidada.TblPedidos.Select(p => p.NumPedido).ToList(),
                FechaEmision = childQuery.Min(r => r.FechaEmision),
                Departamento = depto19?.NombreDepartamento ?? "RECURSOS MATERIALES Y SERVICIOS GENERALES",
                NomResponsableDepartamento = depto19?.NombreJefe ?? childQuery.FirstOrDefault()?.Responsable,
                UsoMaterial = false,
                NumeroFormato = numeroFormato,
                EsConsolidada = true,
                FolioConsolidada = consolidada.FolioConsolidada,
                DepartamentosConsolidada = depto19?.NombreDepartamento,
                Articulos = articulosPendientes.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    ClaveMaterial = a.ClaveMaterial,
                    Cantidad = a.EsDeCompra ? a.CantidadOriginal : a.CantidadMovimiento,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.Descripcion
                }).ToList()
            };

            return View("SalidaMaterialesParaPdf", vm);
        }

        /// <summary>
        /// Genera y muestra el Formato de Entrada de Materiales para una requisición en estatus 7.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> FormatoEntradaPdf(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null) return NotFound();

            var partidasCompra = await _almacenService.ObtenerPartidasCompraParaEntrada(id);

            // Generar o reutilizar el folio ENTRADA
            var numeroFormato = await _almacenService.GenerarFormatoEntrada(id, userId.Value);

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                NumPedidos = dto.NumPedidos,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                UsoMaterial = dto.UsoMaterial,
                NumeroFormato = numeroFormato,
                Articulos = (partidasCompra.Any()
                    ? partidasCompra.Select(a => new ItemRequiVM
                    {
                        IdArticulo = a.IdArticulo,
                        Cog = a.NumPartida,
                        ClaveMaterial = a.ClaveMaterial,
                        Cantidad = a.CantidadComprar,
                        UnidadMedida = a.UnidadMedida,
                        Descripcion = a.Descripcion,
                        DescripcionDetallada = a.Descripcion
                    })
                    : dto.Articulos.Select(a => new ItemRequiVM
                    {
                        IdArticulo = a.IdArticulo,
                        Cog = a.NumPartida,
                        ClaveMaterial = a.ClaveMaterial,
                        Cantidad = a.Cantidad,
                        UnidadMedida = a.UnidadMedida,
                        Descripcion = a.Descripcion,
                        DescripcionDetallada = a.DescripcionDetallada
                    })).ToList()
            };

            return View("EntradaMaterialesParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ConsultarStock(int id)
        {
            var stock = await _almacenService.ConsultarStockParaRequisicion(id);
            return Json(stock);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerEntregasPendientes()
        {
            var data = await _almacenService.ListarEntregasPendientesAlmacen();
            return Json(data);
        }

        /// <summary>
        /// Confirma la entrega f?sica de los art?culos seleccionados.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ConfirmarEntrega([FromForm] ConfirmarEntregaRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            if (request == null || request.IdsMovimientos == null || request.IdsMovimientos.Count == 0)
                return BadRequest(new { ok = false, error = "Debe seleccionar al menos un artículo." });

            if (request.FormatoSalidaFirmado == null || request.FormatoSalidaFirmado.Length == 0)
                return BadRequest(new { ok = false, error = "Debes subir el formato de salida firmado." });

            try
            {
                var extension = Path.GetExtension(request.FormatoSalidaFirmado.FileName)?.ToLowerInvariant();
                if (extension != ".pdf")
                    return BadRequest(new { ok = false, error = "El formato firmado debe ser un archivo PDF." });

                var idRequisicion = request.IdRequisicion;
                var idConsolidada = request.IdConsolidada;

                if (idConsolidada.HasValue)
                {
                    var carpetaRelativa = Path.Combine("uploads", "formato-salida", $"consolidada-{idConsolidada}");
                    var carpetaFisica = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa);
                    Directory.CreateDirectory(carpetaFisica);

                    var nombreArchivo = $"formato_firmado_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                    var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

                    await using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    {
                        await request.FormatoSalidaFirmado.CopyToAsync(stream);
                    }

                    var rutaDb = "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace("\\", "/");
                    var resultado = await _almacenService.ConfirmarEntregaConsolidada(
                        idConsolidada.Value,
                        request.IdsMovimientos,
                        userId.Value,
                        rutaDb);

                    return Json(new { ok = true, mensaje = "Entrega confirmada correctamente." });
                }

                var carpetaRelativa2 = Path.Combine("uploads", "formato-salida", idRequisicion.ToString());
                var carpetaFisica2 = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa2);
                Directory.CreateDirectory(carpetaFisica2);

                var nombreArchivo2 = $"formato_firmado_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                var rutaFisica2 = Path.Combine(carpetaFisica2, nombreArchivo2);

                await using (var stream2 = new FileStream(rutaFisica2, FileMode.Create))
                {
                    await request.FormatoSalidaFirmado.CopyToAsync(stream2);
                }

                var rutaDb2 = "/" + Path.Combine(carpetaRelativa2, nombreArchivo2).Replace("\\", "/");
                var resultado2 = await _almacenService.ConfirmarEntrega(
                    idRequisicion,
                    request.IdsMovimientos,
                    userId.Value,
                    rutaDb2);

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
        public async Task<IActionResult> RegistrarIngreso([FromBody] IngresoInventarioLoteRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            try
            {
                var dtos = (request.Items ?? new()).Select(i => new IngresoInventarioDTO
                {
                    Clave        = i.Clave,
                    Descripcion  = i.Descripcion ?? "",
                    UnidadMedida = i.UnidadMedida ?? "",
                    Cantidad     = i.Cantidad,
                    Motivo       = request.Motivo ?? ""
                }).ToList();

                await _almacenService.RegistrarIngresoInventarioLote(dtos);
                return Json(new { ok = true, mensaje = "Ingreso registrado correctamente.", folio = 0 });
            }
            catch (Exception ex)
            {
                var detalle = ex.InnerException?.InnerException?.Message
                           ?? ex.InnerException?.Message
                           ?? ex.Message;
                return Json(new { ok = false, error = detalle });
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult IngresoInventarioPdf([FromForm] string payload)
        {
            try
            {
                var data = System.Text.Json.JsonSerializer.Deserialize<IngresoInventarioPdfPayload>(
                    payload, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var vm = new VMIngresoInventarioPdf
                {
                    Folio  = data?.Folio ?? 0,
                    Motivo = data?.Motivo,
                    Fecha  = DateTime.Now,
                    Items  = (data?.Items ?? new()).Select(i => new VMIngresoItemPdf
                    {
                        Clave        = i.Clave,
                        Descripcion  = i.Descripcion ?? "",
                        UnidadMedida = i.UnidadMedida ?? "",
                        Cantidad     = i.Cantidad
                    }).ToList()
                };
                return View(vm);
            }
            catch
            {
                return BadRequest("Payload inválido.");
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
                    request.IdRequisicion, userId.Value, entregas, compras, request.EnviarCorreo);
                return Json(new { ok = true, mensaje = "Requisición procesada correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPartidasCompra(int id)
        {
            var partidas = await _almacenService.ObtenerPartidasCompraParaEntrada(id);
            return Json(partidas);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPartidasCompraConsolidada(int idConsolidada)
        {
            var partidas = await _almacenService.ObtenerPartidasCompraConsolidada(idConsolidada);
            return Json(partidas);
        }

        [HttpPost]
        public async Task<IActionResult> GuardarBorradorIngreso([FromBody] GuardarBorradorIngresoRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            try
            {
                if (request.IdConsolidada.HasValue && request.IdConsolidada.Value > 0)
                {
                    await _almacenService.GuardarBorradorIngresoConsolidada(
                        request.IdConsolidada.Value, userId.Value, request.Cantidades ?? new());

                    var urlPdf = Url.Action("FormatoEntradaRealPdfConsolidada", "Almacen",
                        new { idConsolidada = request.IdConsolidada.Value });

                    return Json(new { ok = true, urlPdf });
                }

                await _almacenService.GuardarBorradorIngreso(
                    request.IdRequisicion, userId.Value, request.Cantidades ?? new());

                var urlPdfIndiv = Url.Action("FormatoEntradaRealPdf", "Almacen",
                    new { id = request.IdRequisicion });

                return Json(new { ok = true, urlPdf = urlPdfIndiv });
            }
            catch (Exception ex)
            {
                var mensajeCompleto = ex.InnerException?.Message ?? ex.Message;
                return Json(new { ok = false, error = mensajeCompleto });
            }
        }

        [HttpGet]
        public async Task<IActionResult> FormatoEntradaRealPdf(int id)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null) return NotFound();

            var partidasReales = await _almacenService.ObtenerBorradorIngreso(id);
            if (!partidasReales.Any())
                return BadRequest("No hay borrador guardado para esta requisición.");

            var numeroFormato = await _almacenService.GenerarFormatoEntrada(id, userId.Value);

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                NumPedidos = dto.NumPedidos,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                UsoMaterial = dto.UsoMaterial,
                NumeroFormato = numeroFormato,
                Articulos = partidasReales.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    ClaveMaterial = a.ClaveMaterial,
                    Cantidad = a.CantidadComprar,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.Descripcion
                }).ToList()
            };

            return View("EntradaMaterialesParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> FormatoEntradaRealPdfConsolidada(int idConsolidada)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized();

            var consQuery = await _almacenService.ObtenerPartidasCompraConsolidada(idConsolidada);

            var consolidada = await _almacenService.ObtenerConsolidadaAsync(idConsolidada);
            if (consolidada == null) return NotFound();

            var partidasReales = await _almacenService.ObtenerBorradorIngresoConsolidada(idConsolidada);
            if (!partidasReales.Any())
                return BadRequest("No hay borrador guardado para esta consolidada.");

            var numeroFormato = await _almacenService.GenerarFormatoEntradaConsolidada(idConsolidada, userId.Value);

            var childQuery = await _almacenService.ObtenerChildRequisicionData(idConsolidada);
            var depto19 = await _almacenService.ObtenerDepartamentoRecursosMateriales();

            var vm = new VMRequiForm
            {
                IdRequiMaestra = null,
                NumRequisicion = consolidada.FolioConsolidada,
                NumPedidos = consolidada.TblPedidos.Select(p => p.NumPedido).ToList(),
                FechaEmision = childQuery.Min(r => r.FechaEmision),
                Departamento = depto19?.NombreDepartamento ?? "RECURSOS MATERIALES Y SERVICIOS GENERALES",
                NomResponsableDepartamento = depto19?.NombreJefe ?? childQuery.First().Responsable,
                UsoMaterial = true,
                NumeroFormato = numeroFormato,
                EsConsolidada = true,
                FolioConsolidada = consolidada.FolioConsolidada,
                DepartamentosConsolidada = depto19?.NombreDepartamento ?? string.Join(", ", childQuery.Select(r => r.Departamento).Where(d => d != null).Distinct()),
                Articulos = partidasReales.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    ClaveMaterial = a.ClaveMaterial,
                    Cantidad = a.CantidadComprar,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.Descripcion
                }).ToList()
            };

            return View("EntradaMaterialesParaPdf", vm);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmarIngresoPedido([FromForm] ConfirmarIngresoPedidoRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Unauthorized(new { ok = false, error = "No autorizado." });

            if (request.FormatoEntradaFirmado == null || request.FormatoEntradaFirmado.Length == 0)
                return BadRequest(new { ok = false, error = "Debes subir el formato de entrada firmado." });

            var ext = Path.GetExtension(request.FormatoEntradaFirmado.FileName)?.ToLowerInvariant();
            if (ext != ".pdf")
                return BadRequest(new { ok = false, error = "El formato firmado debe ser un archivo PDF." });

            try
            {
                var esConsolidada = request.IdConsolidada.HasValue && request.IdConsolidada.Value > 0;
                var idFolder = esConsolidada ? "consolidada-" + request.IdConsolidada.Value : request.IdRequisicion.ToString();

                var carpetaRelativa = Path.Combine("uploads", "formato-entrada", idFolder);
                var carpetaFisica = Path.Combine(_webHostEnvironment.WebRootPath, carpetaRelativa);
                Directory.CreateDirectory(carpetaFisica);

                var nombreArchivo = $"entrada_firmada_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}.pdf";
                var rutaFisica = Path.Combine(carpetaFisica, nombreArchivo);

                await using (var stream = new FileStream(rutaFisica, FileMode.Create))
                    await request.FormatoEntradaFirmado.CopyToAsync(stream);

                var rutaDb = "/" + Path.Combine(carpetaRelativa, nombreArchivo).Replace("\\", "/");

                if (esConsolidada)
                {
                    await _almacenService.ConfirmarIngresoPedidoConsolidada(
                        request.IdConsolidada!.Value, userId.Value, rutaDb);
                }
                else
                {
                    await _almacenService.ConfirmarIngresoPedido(
                        request.IdRequisicion, userId.Value, rutaDb, request.EnviarCorreo);
                }

                return Json(new { ok = true, mensaje = "Ingreso registrado correctamente." });
            }
            catch (Exception ex)
            {
                return Json(new { ok = false, error = ex.Message });
            }
        }

        // ─── Registro de Ingresos de Inventario ────────────────────

        [HttpGet]
        public async Task<IActionResult> RegistroIngresos()
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToAction("Index");

            var inventarioDto  = await _almacenService.ObtenerInventario();
            var inventario     = _mapper.Map<List<VMInventarioItem>>(inventarioDto);

            var vm = new VMRegistroIngresosIndex
            {
                Ingresos       = new List<VMIngresoResumen>(), // se conectará cuando el colega agregue las relaciones
                Inventario     = inventario,
                UnidadesMedida = inventario
                    .Select(i => i.UnidadMedida)
                    .Where(u => !string.IsNullOrEmpty(u))
                    .Distinct()
                    .OrderBy(u => u)
                    .ToList()
            };
            return View(vm);
        }

        [HttpGet]
        public IActionResult ObtenerDetalleIngreso(int primerIdIngreso)
        {
            return Json(new List<object>()); // pendiente de relaciones BD
        }

        [HttpPost]
        public IActionResult SubirPdfFirmadoIngreso([FromForm] SubirPdfIngresoRequest request)
        {
            return Json(new { ok = false, error = "Funcionalidad pendiente de configuración de BD." });
        }
    }
}
