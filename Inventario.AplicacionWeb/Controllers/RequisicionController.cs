using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
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

        public RequisicionController(
            IRequisicionesService requisicionesService,
            IMapper mapper, IArticulosService articulosService,
            IUsuarioService usuarioService,
            IMunicipioServie municipioService,
            IProgramaPresupuestarioService programaPresupuestarioService
        )
        {
            _requisicionService = requisicionesService;
            _mapper = mapper;
            _articulosService = articulosService;
            _usuarioService = usuarioService;
            _programaPresupuestarioService = programaPresupuestarioService;
            _municipioService = municipioService;
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
                    .ListarRequisiciones(idDepartamento, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento);
            }

            var actividades = await _programaPresupuestarioService
                .ObtenerActividades();

            var municipios = await _municipioService.ObtenerMunicipios();

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
                }).ToList()
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerUsuariosMateriales()
        {
            var usuarios = await _usuarioService.ListaUsuariosMateriales();
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
                NomDirector = dto.NomDirector,
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
                    DescripcionDetallada = a.DescripcionDetallada
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
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
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

            var requiCreada = await _requisicionService.CrearRequisicion(dto, idUsuario);

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

        public async Task<JsonResult> BuscarArticulos(string term, bool mensual = false)
        {
            var articulos = await _articulosService.BuscarArticulos(term, mensual);

            var resultado = articulos.Select(a => new
            {
                id = a.Id,
                text = a.Descripcion
            }).ToList();

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

            var resultado = await _requisicionService.AtenderRequisicion(
                modelo.IdRequisicion,
                modelo.Observaciones,
                modelo.RequiereModificacion,
                idUsuario,
                modelo.IdPP,
                modelo.FF,
                modelo.TipoPrograma,
                modelo.ClaveRegion,
                modelo.CogsEditados.Select(c => (c.IdArticulo, c.Cog)).ToList()
            );

            if (!resultado) return BadRequest();
            return Ok();
        }
    }
}
