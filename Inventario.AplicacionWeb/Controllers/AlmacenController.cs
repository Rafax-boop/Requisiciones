using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Inventario.AplicacionWeb.Controllers
{
    public class AlmacenController : Controller
    {
        private readonly IRequisicionesService _requisicionService;
        private readonly IGenericRepository<TblInventario> _repoInventario;
        private readonly IGenericRepository<TblEstatus> _repoEstatus;
        private readonly IMapper _mapper;

        public AlmacenController(
            IRequisicionesService requisicionService,
            IGenericRepository<TblInventario> repoInventario,
            IGenericRepository<TblEstatus> repoEstatus,
            IMapper mapper)
        {
            _requisicionService = requisicionService;
            _repoInventario = repoInventario;
            _repoEstatus = repoEstatus;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index()
        {
            var requisicionesDto = await _requisicionService.ListarRequisiciones();
            var requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(requisicionesDto);

            var queryInv = await _repoInventario.Consultar();
            var inventarioEntities = await queryInv.ToListAsync();

            var inventario = inventarioEntities.Select(i => new VMInventarioItem
            {
                Descripcion = i.Descripcion ?? "",
                UnidadMedida = i.UnidadMedida ?? "",
                Existencia = i.Existencia,
                Minimo = 0,
                Situacion = i.Existencia == 0 ? "Sin stock" : "OK"
            }).ToList();

            var unidadesMedida = inventario
                .Select(x => x.UnidadMedida)
                .Where(u => !string.IsNullOrEmpty(u))
                .Distinct()
                .OrderBy(u => u)
                .ToList();

            var queryEst = await _repoEstatus.Consultar();
            var estatusEntities = await queryEst.ToListAsync();
            var estatus = estatusEntities.Select(e => e.NombreEstatus).ToList();

            var vm = new VMAlmacenIndex
            {
                Requisiciones = requisiciones,
                Inventario = inventario,
                Estatus = estatus,
                UnidadesMedida = unidadesMedida
            };

            return View(vm);
        }

        /// <summary>
        /// Devuelve la requisición completa en JSON para el modal de análisis (maqueta).
        /// </summary>
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
