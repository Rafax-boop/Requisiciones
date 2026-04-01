using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.Entity.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize]
    public class RequisicionController : Controller
    {
        private readonly IRequisicionesService _requisicionService;
        private readonly IMapper _mapper;
        private readonly IArticulosService _articulosService;
        private readonly IUsuarioService _usuarioService;
        private readonly IMunicipioServie _municipioService;
        private readonly IProgramaPresupuestarioService _programaPresupuestarioService;
        private readonly IAlmacenService _almacenService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RequisicionController(
            IRequisicionesService requisicionesService,
            IMapper mapper, IArticulosService articulosService,
            IUsuarioService usuarioService,
            IMunicipioServie municipioService,
            IProgramaPresupuestarioService programaPresupuestarioService,
            IAlmacenService almacenService,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _requisicionService = requisicionesService;
            _mapper = mapper;
            _articulosService = articulosService;
            _usuarioService = usuarioService;
            _programaPresupuestarioService = programaPresupuestarioService;
            _municipioService = municipioService;
            _almacenService = almacenService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> FormularioRequisiciones([FromQuery] string? tipo = "general")
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            if (tipo != "mensual" && tipo != "servicios")
                tipo = "general";

            ViewBag.TipoRequisicion = tipo;

            var dto = await _usuarioService.ObtenerDatosDepartamento(idUsuario);

            var vm = _mapper.Map<VMRequiForm>(dto);

            ViewBag.UsaFlujoContinuar = true;

            return View(vm);
        }

        public async Task<IActionResult> TablaRequisiciones()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out int idDepartamento))
                return RedirectToAction("Login", "Acceso");

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("3"))
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false);
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
        public async Task<JsonResult> ObtenerUsuariosMateriales()
        {
            var usuarios = await _usuarioService.ListaUsuariosAsignar(3);
            var resultado = usuarios.Select(u => new
            {
                id = u.IdUsuario,
                nombre = u.Usuario
            }).ToList();
            return Json(resultado);
        }

        public async Task<JsonResult> ObtenerDetalles(int idMaestro)
        {
            var detalles = await _requisicionService.ObtenerDetallePorIdMaestro(idMaestro);
            return Json(detalles);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProgresoRequisicion(int idRequisicion)
        {
            var pasos = await _requisicionService.ObtenerProgresoRequisicion(idRequisicion);
            return Json(pasos);
        }

        [HttpGet]
        public async Task<IActionResult> VerParaPdf(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

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
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                UsoEspecifico = dto.UsoEspecifico,
                Justificacion = dto.Justificacion,
                CuentaProgramaPresupuestario = dto.CuentaProgramaPresupuestario,
                UsoMaterial = dto.UsoMaterial,
                Hash = dto.Hash,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada,
                    TipoProgramacion = a.TipoProgramacion,
                    Llenado1 = a.Llenado1,
                    Llenado2 = a.Llenado2,
                    Llenado3 = a.Llenado3,
                    Llenado4 = a.Llenado4,
                    Llenado5 = a.Llenado5,
                    Llenado6 = a.Llenado6,
                    Llenado7 = a.Llenado7,
                    Llenado8 = a.Llenado8,
                    Llenado9 = a.Llenado9,
                    Llenado10 = a.Llenado10,
                    Llenado11 = a.Llenado11,
                    Llenado12 = a.Llenado12,
                    Mes = a.Mes
                }).ToList()
            };

            return View("RequisicionParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditarRequisicion(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                UsoEspecifico = dto.UsoEspecifico,
                Justificacion = dto.Justificacion,
                CuentaProgramaPresupuestario = dto.CuentaProgramaPresupuestario,
                UsoMaterial = dto.UsoMaterial,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    ClaveMaterial = a.ClaveMaterial,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada
                }).ToList()
            };

            vm.ObservacionesBitacora = await _requisicionService.ObtenerObservacionesModificacion(id);

            ViewBag.ModoEdicion = true;
            return View("FormularioRequisiciones", vm);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionService.ActualizarRequisicion(modelo.IdRequiMaestra!.Value, dto, idUsuario);

            if (exito)
            {
                TempData["MensajeExito"] = "Requisición editada correctamente.";
                return RedirectToAction("TablaRequisiciones", "Requisicion");
            }

            ViewBag.ModoEdicion = true;
            return View("FormularioRequisiciones", modelo);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            var requiCreada = await _requisicionService.CrearRequisicion(dto, idUsuario, false);

            TempData["MensajeExito"] = "Requisición guardada correctamente.";
            TempData["FolioCreado"] = requiCreada.NumRequisicion;
            return RedirectToAction("TablaRequisiciones", "Requisicion");
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerInfoArticulo(int id)
        {
            var articulo = await _articulosService.ObtenerArticuloPorId(id);
            return Json(new
            {
                id = articulo.Id,
                cog = articulo.Cog,
                clave = articulo.ClaveMaterial,
                unidadMedida = articulo.UnidadMedida ?? "-",
                descripcion = articulo.Descripcion
            });
        }

        public async Task<JsonResult> BuscarArticulos(string term, string tipo = "normal")
        {
            TipoBusquedaArticulo tipoBusqueda = tipo switch
            {
                "mensual" => TipoBusquedaArticulo.Mensual,
                _ => TipoBusquedaArticulo.Normal
            };

            var articulos = await _articulosService.BuscarArticulos(term, tipoBusqueda);

            var resultado = articulos.Select(a => new
            {
                id = a.Id,
                text = a.Descripcion
            });

            return Json(resultado);
        }

        [HttpGet]
        public async Task<JsonResult> BuscarCogs(string term)
        {
            var cogs = await _articulosService.BuscarCogs(term);
            return Json(cogs.Select(c => new { id = c, text = c.ToString() }));
        }

        [HttpPost]
        public async Task<JsonResult> AsignarRequisicion(int idRequi, int idUsuario)
        {
            int idUsuarioLog = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.AsignarRequisicion(idRequi, idUsuarioLog, idUsuario);
                return Json(new { success = resultado });
            }
            catch
            {
                return Json(new { success = false, mensaje = "Error al asignar la requisición" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Atender([FromBody] VMAtenderRequisicion modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var dto = _mapper.Map<AtenderRequiDTO>(modelo);

            var resultado = await _requisicionService.AtenderRequisicion(dto, idUsuario);

            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpPost]
        public async Task<JsonResult> EnviarAAlmacen(int idRequi)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.EnviarAAlmacen(idRequi, idUsuario);
                return Json(new { success = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message, detalle = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarAModificacion(int idRequi, string observaciones)
        {
            try
            {
                var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var resultado = await _requisicionService.EnviarAModificacion(idRequi, idUsuario, observaciones);

                if (!resultado)
                    return Json(new { success = false, mensaje = "No se encontró la requisición." });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = "Error interno.", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> RechazarRequisicion(int idRequi, string motivo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.RechazarRequisicion(idRequi, idUsuario, motivo);
                return Json(new { success = resultado });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubirArchivosAtencion(
            int IdRequisicion,
            List<IFormFile>? Cotizaciones,
            List<IFormFile>? CuadroComparativo)
        {
            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await _requisicionService.GuardarArchivosAtencion(
                IdRequisicion,
                Cotizaciones ?? new List<IFormFile>(),
                CuadroComparativo ?? new List<IFormFile>(),
                webRootPath
            );
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SubirDocumentoProveedor(
    [FromForm] int idRequisicion,
    [FromForm] string tipoDocumento,
    IFormFile archivo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.SubirDocumentoProveedor(
                idRequisicion, tipoDocumento, archivo,
                _webHostEnvironment.WebRootPath, idUsuario);

            return ok ? Ok(new { success = true })
                      : BadRequest(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDocumentosProveedor(int idRequisicion)
        {
            var docs = await _requisicionService.ObtenerDocumentosProveedor(idRequisicion);
            return Ok(docs);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarAFinancierosConDocs([FromForm] int idRequisicion)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.EnviarAFinancierosConDocs(idRequisicion, idUsuario);
            return Ok(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> RebotarDocumentos([FromBody] RebotarDocumentosDTO modelo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.RebotarDocumentos(
                modelo.IdRequisicion, modelo.Observaciones,
                modelo.DocumentosObservados, idUsuario);
            return Ok(new { success = ok });
        }
    }
}
