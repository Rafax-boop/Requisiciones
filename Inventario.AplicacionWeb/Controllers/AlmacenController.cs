using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            var requisicionesDto = await _almacenService.ListarRequisicionesAutorizadas();
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
    }
}
