using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class FinancierosController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IFinancierosService _financierosService;
        private readonly IRequisicionesService _requisicionesService;
        private readonly IUsuarioService _usuarioService;
        private readonly ICatalogoService _catalogoService;
        private readonly IAlmacenService _almacenService;
        private readonly ILogger<FinancierosController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IConsolidadaService _consolidadaService;


        public FinancierosController(IMapper mapper,
            IFinancierosService financierosService,
            IRequisicionesService requisicionesService,
            IUsuarioService usuarioService,
            ICatalogoService catalogoService,
            IAlmacenService almacenService,
            ILogger<FinancierosController> logger,
            IWebHostEnvironment env,
            IConsolidadaService consolidadaService)
        {
            _mapper = mapper;
            _financierosService = financierosService;
            _requisicionesService = requisicionesService;
            _usuarioService = usuarioService;
            _catalogoService = catalogoService;
            _almacenService = almacenService;
            _logger = logger;
            _env = env;
            _consolidadaService = consolidadaService;
        }

        [HttpGet]
        public async Task<IActionResult> TablaFinancieros()
        {
            ViewData["Title"] = "TablaFinancieros";
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out int idDepartamento))
                return RedirectToAction("Login", "Acceso");

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("9"))
            {
                listaDTO = await _financierosService
                    .ListarRequisiciones(idUsuario);
            }
            else
            {
                listaDTO = await _financierosService
                    .ListarRequisiciones();
            }

            listaDTO = listaDTO
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequi)
                .ToList();

            var actividades = await _catalogoService
                .ObtenerActividades();
            var fuentesFinanciamiento = await _catalogoService.ObtenerFuentesFinanciamiento();
            var estatus = await _almacenService.ObtenerEstatus();

            var vm = new VMTablaRequisiciones
            {
                Requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO),

                ListaActividades = actividades.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Nombre
                }).ToList(),

                ListaFuentesFinanciamiento = fuentesFinanciamiento.Select(f => new SelectListItem
                {
                    Value = f.Clave,
                    Text = f.Nombre
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerUsuariosFinancieros()
        {
            var usuarios = await _usuarioService.ListaUsuariosAsignar(9);
            var resultado = usuarios.Select(u => new
            {
                id = u.IdUsuario,
                nombre = u.Usuario
            }).ToList();
            return Json(resultado);
        }

        [HttpPost]
        public async Task<JsonResult> AsignarRequisicion(int idRequi, int idUsuario)
        {
            int idUsuarioLog = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _financierosService.AsignarRequisicion(idRequi, idUsuarioLog, idUsuario);
                return Json(new { success = resultado });
            }
            catch
            {
                return Json(new { success = false, mensaje = "Error al asignar la requisición" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerConsolidadasFinancieros()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idUsuarioClaim, out int idUsuario);

            List<ConsolidadaFinancierosDTO> lista;
            if (User.IsInRole("9"))
                lista = await _financierosService.ListarConsolidadasFinancieros(idUsuario);
            else
                lista = await _financierosService.ListarConsolidadasFinancieros();

            return Json(lista);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerConsolidadasAutorizadasFinancieros()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idUsuarioClaim, out int idUsuario);

            var estatusAutorizados = new List<int> { 15, 7, 12 };
            List<ConsolidadaFinancierosDTO> lista;
            if (User.IsInRole("9"))
                lista = await _financierosService.ListarConsolidadasFinancieros(idUsuario, estatusAutorizados);
            else
                lista = await _financierosService.ListarConsolidadasFinancieros(null, estatusAutorizados);

            return Json(lista);
        }

        [HttpPost]
        public async Task<JsonResult> AsignarConsolidada(int idConsolidada, int idUsuario)
        {
            int idUsuarioLog = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _financierosService.AsignarConsolidada(idConsolidada, idUsuarioLog, idUsuario);
                return Json(new { success = resultado });
            }
            catch
            {
                return Json(new { success = false, mensaje = "Error al asignar la consolidada" });
            }
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDetalleConsolidadaFinancieros(int idConsolidada)
        {
            var detalle = await _consolidadaService.ObtenerDetalleConsolidada(idConsolidada);
            var consolidada = await _consolidadaService.ObtenerConsolidada(idConsolidada);

            if (consolidada != null)
            {
                detalle.IdEstatus = consolidada.IdEstatus;
                detalle.IdUsuarioFinan = consolidada.IdUsuarioFinan;
                detalle.DiasAsignado = consolidada.IdUsuarioFinan != null
                    ? (DateTime.Now - (consolidada.FechaModificacion ?? DateTime.Now)).Days
                    : 0;
                detalle.IdPp = consolidada.IdPp;
                detalle.Ff = consolidada.Ff;
                detalle.TipoPrograma = consolidada.TipoPrograma;

                if (consolidada.IdUsuarioFinan.HasValue)
                {
                    var usuarios = await _usuarioService.ListaUsuariosAsignar(9);
                    var analista = usuarios.FirstOrDefault(u => u.IdUsuario == consolidada.IdUsuarioFinan.Value);
                    detalle.NombreAsignado = analista?.Usuario;
                }
            }

            return Json(detalle);
        }

        [HttpPost]
        public async Task<IActionResult> Atender([FromForm] AtenderRequiDTO modelo)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var resultado = await _financierosService.AtenderRequisicion(modelo, idUsuario);
            if (!resultado.Exito) return BadRequest();
            return Ok(new { success = true, numPedido = resultado.NumPedido });
        }

        [HttpPost]
        public async Task<IActionResult> AtenderConsolidada([FromForm] AtenderConsolidadaDTO modelo)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var resultado = await _financierosService.AtenderConsolidadaFinancieros(modelo, idUsuario);
            if (!resultado.Exito) return BadRequest();
            return Ok(new { success = true, numPedido = resultado.NumPedido });
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarRequisicion([FromForm] FinalizarRequisicionRequestDto modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            bool resultado;
            if (modelo.IdConsolidada.HasValue)
                resultado = await _financierosService.FinalizarRequisicionConsolidada(
                    modelo.IdConsolidada.Value, modelo.Transferencia, idUsuario);
            else
                resultado = await _financierosService.FinalizarRequisicion(
                    modelo.IdRequisicion!.Value, modelo.Transferencia, idUsuario);
            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RebotarDocumentos([FromBody] RebotarDocumentosDTO modelo)
        {
            int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            bool ok;
            if (modelo.IdConsolidada.HasValue)
                ok = await _financierosService.RebotarDocumentosConsolidada(
                    modelo.IdConsolidada.Value, modelo.Observaciones,
                    modelo.DocumentosObservados, idUsuario);
            else
                ok = await _requisicionesService.RebotarDocumentos(
                    modelo.IdRequisicion!.Value, modelo.Observaciones,
                    modelo.DocumentosObservados, idUsuario);
            return Ok(new { success = ok });
        }

        [HttpGet]
        public async Task<IActionResult> DescargarTablaApi(int idRequisicion)
        {
            try
            {
                var numApi = await _financierosService.AsegurarNumeroApiAsync(idRequisicion);
                var bytes = await _financierosService.GenerarTablaApiAsync(idRequisicion);

                var nombreArchivo = $"TablaAPI_{idRequisicion}_{DateTime.Now:yyyyMMdd}.pdf";

                // FileResult con inline para abrir en el navegador,
                // o usar "attachment" para forzar descarga directa.
                return File(bytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                // Esto imprime el error REAL en la consola / logs de tu servidor ASP.NET
                _logger?.LogError(ex, "Error generando Tabla API para requisición {Id}", idRequisicion);

                // Respuesta legible si el usuario abre la URL directamente
                return StatusCode(500, $"Error al generar el PDF: {ex.Message}\n\n{ex.StackTrace}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarTablaApi(int idRequisicion)
        {
            try
            {
                var modelo = await _financierosService.ObtenerTablaApiEditableAsync(idRequisicion);
                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error preparando edición de Tabla API para requisición {Id}", idRequisicion);
                return RedirectToAction(nameof(TablaFinancieros));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarTablaApiConsolidada(int idConsolidada)
        {
            try
            {
                var modelo = await _financierosService.ObtenerTablaApiEditableConsolidadaAsync(idConsolidada);
                return View("EditarTablaApi", modelo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error preparando edición de Tabla API para consolidada {Id}", idConsolidada);
                return RedirectToAction(nameof(TablaFinancieros));
            }
        }

        [HttpGet]
        public async Task<IActionResult> DescargarTablaApiConsolidada(int idConsolidada)
        {
            try
            {
                var numApi = await _financierosService.AsegurarNumeroApiConsolidadaAsync(idConsolidada);
                var modelo = await _financierosService.ObtenerTablaApiEditableConsolidadaAsync(idConsolidada);
                var bytes = await _financierosService.GenerarTablaApiAsync(modelo);

                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _financierosService.GuardarHistorialTablaApiAsync(
                    modelo,
                    idUsuario,
                    observacion: $"PDF generado (descarga directa) el {DateTime.Now:dd/MM/yyyy HH:mm}");

                var nombreArchivo = $"TablaAPI_Consolidada_{idConsolidada}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generando Tabla API para consolidada {Id}", idConsolidada);
                return StatusCode(500, $"Error al generar el PDF: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerNumeroApi(int idRequisicion)
        {
            try
            {
                var numApi = await _financierosService.AsegurarNumeroApiAsync(idRequisicion);
                return Ok(new { numApi });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerNumeroApiConsolidada(int idConsolidada)
        {
            try
            {
                var numApi = await _financierosService.AsegurarNumeroApiConsolidadaAsync(idConsolidada);
                return Ok(new { numApi });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarTablaApiEditada([FromForm] TablaApiEditableDTO modelo)
        {
            try
            {
                if (modelo.IdRequisicion <= 0 && (modelo.IdConsolidada == null || modelo.IdConsolidada <= 0))
                    return BadRequest("La requisición o consolidada es requerida.");

                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                if (modelo.IdConsolidada > 0)
                    await _financierosService.AsegurarNumeroApiConsolidadaAsync(modelo.IdConsolidada.Value);
                else
                    await _financierosService.AsegurarNumeroApiAsync(modelo.IdRequisicion);

                var bytes = await _financierosService.GenerarTablaApiAsync(modelo);

                await _financierosService.GuardarHistorialTablaApiAsync(
                    modelo,
                    idUsuario,
                    observacion: $"PDF generado el {DateTime.Now:dd/MM/yyyy HH:mm}");

                var sufijo = modelo.IdConsolidada > 0
                    ? $"Consolidada_{modelo.IdConsolidada}"
                    : $"{modelo.IdRequisicion}";
                var nombreArchivo = $"TablaAPI_Editada_{sufijo}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "sin inner";
                return StatusCode(500, $"Error: {ex.Message} | Inner: {inner}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> HistorialTablaApi(int idRequisicion = 0, int idConsolidada = 0)
        {
            if (idConsolidada > 0)
            {
                var consolidada = await _consolidadaService.ObtenerConsolidada(idConsolidada);
                var vm = new HistorialDocumentosVM
                {
                    IdConsolidada = idConsolidada,
                    NumRequisicion = consolidada?.FolioConsolidada ?? "",
                    HistorialTablaApi = await _financierosService.ObtenerHistorialTablaApiPorConsolidadaAsync(idConsolidada),
                    HistorialPedido = await _financierosService.ObtenerHistorialPedidoPorConsolidadaAsync(idConsolidada)
                };
                return View(vm);
            }

            var requi = await _requisicionesService.ObtenerRequisicionCompletaPorId(idRequisicion);
            var vm2 = new HistorialDocumentosVM
            {
                IdRequisicion = idRequisicion,
                NumRequisicion = requi?.NumRequisicion ?? "",
                HistorialTablaApi = await _financierosService.ObtenerHistorialTablaApiAsync(idRequisicion),
                HistorialPedido = await _financierosService.ObtenerHistorialPedidoAsync(idRequisicion)
            };
            return View(vm2);
        }

        [HttpGet]
        public async Task<IActionResult> EditarPedidoCompra(int idRequisicion)
        {
            try
            {
                var modelo = await _financierosService.ObtenerPedidoEditableAsync(idRequisicion);
                return View(modelo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cargando pedido editable para requisición {Id}", idRequisicion);
                return RedirectToAction(nameof(TablaFinancieros));
            }
        }

        [HttpGet]
        public async Task<IActionResult> EditarPedidoCompraConsolidada(int idConsolidada)
        {
            try
            {
                var modelo = await _financierosService.ObtenerPedidoEditableConsolidadaAsync(idConsolidada);
                return View("EditarPedidoCompra", modelo);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error cargando pedido editable para consolidada {Id}", idConsolidada);
                return RedirectToAction(nameof(TablaFinancieros));
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPedidoPdf([FromForm] PedidoVistaDTO form)
        {
            try
            {
                var bytes = await _financierosService.GenerarPedidoPdfAsync(form, _env.WebRootPath);

                // ── NUEVO: guardar historial igual que Tabla API ──────────────
                int idUsuario = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await _financierosService.GuardarHistorialPedidoAsync(
                    form,
                    idUsuario,
                    observacion: $"Pedido PDF generado el {DateTime.Now:dd/MM/yyyy HH:mm}");
                // ─────────────────────────────────────────────────────────────

                var nombre = $"Pedido_{form.IdRequisicion ?? 0}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generando PDF de pedido para requisición {Id}", form.IdRequisicion ?? 0);
                return StatusCode(500, $"Error al generar el PDF: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> GenerarPedidoPdfConsolidada([FromForm] PedidoVistaDTO form)
        {
            try
            {
                var bytes = await _financierosService.GenerarPedidoPdfAsync(form, _env.WebRootPath);

                int idUsuario = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                await _financierosService.GuardarHistorialPedidoConsolidadaAsync(
                    form,
                    idUsuario,
                    observacion: $"Pedido PDF generado el {DateTime.Now:dd/MM/yyyy HH:mm}");

                var nombre = $"Pedido_Consolidada_{form.IdConsolidada ?? 0}_{DateTime.Now:yyyyMMdd}.pdf";
                return File(bytes, "application/pdf", nombre);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generando PDF de pedido para consolidada {Id}", form.IdConsolidada ?? 0);
                return StatusCode(500, $"Error al generar el PDF: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarFinancierosConsolidada(int idConsolidada, IFormFile? archivo)
        {
            try
            {
                int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var (success, message) = await _financierosService.EnviarFinancierosConsolidadaAsync(idConsolidada, idUsuario, archivo, _env.WebRootPath);
                if (!success)
                    return Ok(new { success, message });
                return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error enviando consolidada {Id} a financieros", idConsolidada);
                return Ok(new { success = false, message = $"Error: {ex.Message}" });
            }
        }

        /// <summary>IDs por pestaña para notificaciones (sondeo en cliente).</summary>
        [HttpGet]
        public async Task<IActionResult> SnapshotIdsPorTab()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out _))
                return Unauthorized();

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return Unauthorized();

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("9"))
            {
                listaDTO = await _financierosService.ListarRequisiciones(idUsuario);
            }
            else
            {
                listaDTO = await _financierosService.ListarRequisiciones();
            }

            listaDTO = listaDTO
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequi)
                .ToList();

            var principal = listaDTO.Where(r => r.IdEstatus == 13 || r.IdEstatus == 14).Select(r => r.IdRequi).ToList();
            var autorizadas = listaDTO.Where(r => r.IdEstatus == 15).Select(r => r.IdRequi).ToList();
            var rechazadas = listaDTO.Where(r => r.IdEstatus == 5).Select(r => r.IdRequi).ToList();
            var procesopago = listaDTO.Where(r => r.IdEstatus == 17).Select(r => r.IdRequi).ToList();

            // incluir IDs de consolidadas para notificaciones
            var consolidadas = await _financierosService.ListarConsolidadasFinancieros(
                User.IsInRole("9") ? idUsuario : null,
                new List<int> { 13, 14, 15, 17 });

            var consPrincipal = consolidadas.Where(c => c.IdEstatus == 13 || c.IdEstatus == 14)
                .Select(c => c.ConsolidadaId * -1).ToList(); // negativo para distinguir de individuales
            var consAutorizadas = consolidadas.Where(c => c.IdEstatus == 15)
                .Select(c => c.ConsolidadaId * -1).ToList();
            var consProcesoPago = consolidadas.Where(c => c.IdEstatus == 17)
                .Select(c => c.ConsolidadaId * -1).ToList();

            principal.AddRange(consPrincipal);
            autorizadas.AddRange(consAutorizadas);
            procesopago.AddRange(consProcesoPago);

            return Json(new { principal, autorizadas, rechazadas, procesopago });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerProcesoPago()
        {
            List<RequisicionMaestraDTO> listaDTO;
            int? idUsuario = User.IsInRole("9")
                ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
                : null;

            if (idUsuario.HasValue)
                listaDTO = await _financierosService.ListarRequisiciones(idUsuario.Value);
            else
                listaDTO = await _financierosService.ListarRequisiciones();

            var individuales = listaDTO
                .Where(r => r.IdEstatus == 17)
                .Select(r => new
                {
                    id = r.IdRequi,
                    folio = r.NumRequi,
                    fecha = r.FechaEmision?.ToString("dd/MM/yyyy") ?? "",
                    departamento = r.Departamento,
                    responsable = r.Responsable,
                    partidas = r.CantidadPartidas,
                    estatus = r.Estatus,
                    idEstatus = r.IdEstatus,
                    esConsolidada = false
                }).ToList();

            var consolidadas = await _financierosService.ListarConsolidadasFinancieros(
                idUsuario, new List<int> { 17 });

            var itemsConsolidada = consolidadas.Select(c => new
            {
                id = c.ConsolidadaId,
                folio = c.FolioConsolidada,
                fecha = c.FechaCreacion,
                departamento = c.Departamentos,
                responsable = "",
                partidas = c.CantidadRequis,
                estatus = c.Estatus,
                idEstatus = c.IdEstatus,
                esConsolidada = true
            }).ToList();

            var combinado = individuales.Concat(itemsConsolidada)
                .OrderBy(x => x.fecha)
                .ToList();

            return Json(combinado);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerRequisicionesConArchivos()
        {
            List<RequisicionMaestraDTO> listaDTO;
            if (User.IsInRole("9"))
            {
                var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(idUsuarioClaim, out int idUsuario);
                listaDTO = await _requisicionesService.ObtenerRequisicionesConArchivosTodos(null, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionesService.ObtenerRequisicionesConArchivosTodos(null);
            }

            var requisiciones = listaDTO.Select(r => new
            {
                idRequi = r.IdRequi,
                numRequi = r.NumRequi,
                fechaEmision = r.FechaEmision.ToString(),
                departamento = r.Departamento,
                responsable = r.Responsable,
                cantidadPartidas = r.CantidadPartidas,
                estatus = r.Estatus
            }).ToList();

            return Json(new { requisiciones });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerConsolidadasConArchivos()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int.TryParse(idUsuarioClaim, out int idUsuario);

            List<ConsolidadaDTO> lista;
            if (User.IsInRole("9"))
                lista = await _consolidadaService.ObtenerConsolidadasConArchivos(null, null, idUsuario);
            else
                lista = await _consolidadaService.ObtenerConsolidadasConArchivos();

            var consolidadas = lista.Select(c => new
            {
                consolidadaId = c.ConsolidadaID,
                folioConsolidada = c.FolioConsolidada,
                fechaCreacion = c.FechaCreacion,
                cantidadRequis = c.CantidadRequis,
                totalPartidas = c.TotalPartidas,
                departamentos = c.Departamentos,
                idEstatus = c.IdEstatus,
                estatus = c.Estatus
            }).ToList();

            return Json(new { consolidadas });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerTodosLosArchivos(int idRequisicion)
        {
            var archivos = await _requisicionesService.ObtenerTodosLosArchivosDeRequisicion(idRequisicion);
            var resultado = archivos.Select(a => new
            {
                id = a.Id,
                tipo = a.Tipo,
                ruta = a.Ruta,
                fechaSubida = a.FechaSubida?.ToString("dd/MM/yyyy HH:mm") ?? "",
                nombreArchivo = GenerarNombreArchivoCorto(a.Tipo, idRequisicion, a.Ruta)
            }).ToList();
            return Json(resultado);
        }

        private string GenerarNombreArchivoCorto(string tipo, int idRequisicion, string ruta)
        {
            var extension = Path.GetExtension(ruta);
            var tipoAbreviado = ObtenerAbreviaturaTipo(tipo);
            return $"Requi-{idRequisicion}-{tipoAbreviado}{extension}";
        }

        private string ObtenerAbreviaturaTipo(string tipo)
        {
            if (string.IsNullOrEmpty(tipo)) return "Doc";

            return tipo.ToLower() switch
            {
                var t when t.Contains("transferencia") => "Transf",
                var t when t.Contains("cfdi") => "CFDI",
                var t when t.Contains("memo") || t.Contains("pago") => "Memo",
                var t when t.Contains("domicilio") => "CompDom",
                var t when t.Contains("acta") => "Acta",
                var t when t.Contains("factura") => "Fact",
                var t when t.Contains("pedido") => "Pedido",
                var t when t.Contains("cotizacion") || t.Contains("cotización") => "Cotiz",
                _ => tipo.Replace("proveedor_", "").Substring(0, Math.Min(5, tipo.Length))
            };
        }
    }
}
