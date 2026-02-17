using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.AplicacionWeb.Controllers
{
    public class RequisicionController : Controller
    {
        private readonly IRequisicionesService _requisicionService;
        private readonly IMapper _mapper;
        private readonly IArticulosService _articulosService;

        public RequisicionController(IRequisicionesService requisicionesService, IMapper mapper, IArticulosService articulosService)
        {
            _requisicionService = requisicionesService;
            _mapper = mapper;
            _articulosService = articulosService;
        }

        public IActionResult FormularioRequisiciones()
        {
            return View();
        }

        public IActionResult TablaRequisiciones()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo)
        {
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionService.CrearRequisicion(dto);

            if (exito)
            {
                return RedirectToAction("Index", "Requisicion");
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
