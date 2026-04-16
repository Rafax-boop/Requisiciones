using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
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
        private readonly IUsuarioService _usuarioService;
        private readonly IProgramaPresupuestarioService _programaPresupuestarioService;
        private readonly IMunicipioServie _municipioService;
        private readonly IAlmacenService _almacenService;
        private readonly ILogger<FinancierosController> _logger;


        public FinancierosController(IMapper mapper,
            IFinancierosService financierosService,
            IUsuarioService usuarioService,
            IProgramaPresupuestarioService programaPresupuestarioService,
            IMunicipioServie municipioService,
            IAlmacenService almacenService,
            ILogger<FinancierosController> logger)
        {
            _mapper = mapper;
            _financierosService = financierosService;
            _usuarioService = usuarioService;
            _programaPresupuestarioService = programaPresupuestarioService;
            _municipioService = municipioService;
            _almacenService = almacenService;
            _logger = logger;
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

            var actividades = await _programaPresupuestarioService
                .ObtenerActividades();

            var municipios = await _municipioService.ObtenerMunicipios();
            var estatus = await _almacenService.ObtenerEstatus();

            var vm = new VMTablaRequisiciones
            {
                Requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO),

                ListaActividades = actividades.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.DescripcionActividad
                }).ToList(),

                ListaMunicipios = municipios.Select(m => new SelectListItem
                {
                    Value = m.Id.ToString(),
                    Text = m.Municipio
                }).ToList(),
                Estatus = estatus
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

        [HttpPost]
        public async Task<IActionResult> Atender([FromForm] VMAtenderRequisicion modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var dto = _mapper.Map<AtenderRequiDTO>(modelo);

            var resultado = await _financierosService.AtenderRequisicion(dto, idUsuario);

            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> FinalizarRequisicion([FromForm] VMRevisarRequisicion modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var resultado = await _financierosService.FinalizarRequisicion(modelo.IdRequisicion, modelo.Transferencia, idUsuario);
            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> DescargarTablaApi(int idRequisicion)
        {
            try
            {
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerarTablaApiEditada([FromForm] TablaApiEditableDTO modelo)
        {
            try
            {
                if (modelo.IdRequisicion <= 0)
                    return BadRequest("La requisición es requerida.");

                // Obtener usuario de sesión (ajusta según tu implementación)
                int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var bytes = await _financierosService.GenerarTablaApiAsync(modelo);

                // Guardar historial antes de devolver
                await _financierosService.GuardarHistorialTablaApiAsync(
                    modelo,
                    idUsuario,
                    observacion: $"PDF generado el {DateTime.Now:dd/MM/yyyy HH:mm}");

                var nombreArchivo = $"TablaAPI_Editada_{modelo.IdRequisicion}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
                return File(bytes, "application/pdf", nombreArchivo);
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? "sin inner";
                return StatusCode(500, $"Error: {ex.Message} | Inner: {inner}");
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

            return Json(new { principal, autorizadas, rechazadas, procesopago });
        }
    }
}
