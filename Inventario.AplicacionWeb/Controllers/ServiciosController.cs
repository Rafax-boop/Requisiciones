using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;
        private readonly IRequisicionesService _requisicionesService;
        private readonly IArticulosService _articulosService;
        private readonly IAlmacenService _almacenService;
        private readonly IProgramaPresupuestarioService _programaService;
        private readonly IMunicipioServie _municipioService;

        public ServiciosController(
            IUsuarioService usuarioService,
            IMapper mapper,
            IRequisicionesService requisicionesService,
            IArticulosService articulosService,
            IAlmacenService almacenService,
            IMunicipioServie municipioService,
            IProgramaPresupuestarioService programaService
        )
        {
            _usuarioService = usuarioService;
            _mapper = mapper;
            _requisicionesService = requisicionesService;
            _articulosService = articulosService;
            _almacenService = almacenService;
            _municipioService = municipioService;
            _programaService = programaService;
        }

        [HttpGet]
        public async Task<IActionResult> TablaRequisicionServicios()
        {
            ViewData["Title"] = "TablaRequisicionServicios";
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
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true);
            }

            listaDTO = listaDTO.OrderBy(r => r.FechaModificacion).ToList();

            var actividades = await _programaService
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
        public async Task<IActionResult> FormularioRequisicionServicios()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            ViewData["Title"] = "FormularioRequisicionServicios";

            var dto = await _usuarioService.ObtenerDatosDepartamento(idUsuario);
            var vm = _mapper.Map<VMRequiForm>(dto);

            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            var requiCreada = await _requisicionesService.CrearRequisicion(dto, idUsuario, true);

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
    }
}
