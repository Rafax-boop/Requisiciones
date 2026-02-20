using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class RequisicionController : Controller
    {
        private readonly IRequisicionesService _requisicionService;
        private readonly IMapper _mapper;
        private readonly IArticulosService _articulosService;
        private readonly IUsuarioService _usuarioService;

        public RequisicionController(IRequisicionesService requisicionesService, IMapper mapper, IArticulosService articulosService, IUsuarioService usuarioService)
        {
            _requisicionService = requisicionesService;
            _mapper = mapper;
            _articulosService = articulosService;
            _usuarioService = usuarioService;
        }

        public async Task<IActionResult> FormularioRequisiciones()
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var dto = await _usuarioService.ObtenerDatosDepartamento(idUsuario);

            var vm = _mapper.Map<VMRequiForm>(dto);

            vm.FechaEmision = DateTime.Now;

            return View(vm);
        }

        public async Task<IActionResult> TablaRequisiciones()
        {
            var listaDTO = await _requisicionService.ListarRequisiciones();
            var viewModel = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO);
            return View(viewModel);
        }

        public async Task<JsonResult> ObtenerDetalles(int idMaestro)
        {
            var detalles = await _requisicionService.ObtenerDetallePorIdMaestro(idMaestro);
            return Json(detalles);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionService.CrearRequisicion(dto, idUsuario);

            if (exito)
            {
                return RedirectToAction("TablaRequisiciones", "Requisicion");
            }
            else
            {
                return View(modelo);
            }
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

        public async Task<JsonResult> BuscarArticulos(string term)
        {
            var articulos = await _articulosService.BuscarArticulos(term);

            var resultado = articulos.Select(a => new
            {
                id = a.Id,
                text = a.Descripcion
            }).ToList();

            return Json(resultado);
        }
    }
}
