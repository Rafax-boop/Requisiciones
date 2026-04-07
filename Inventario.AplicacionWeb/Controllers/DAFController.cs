using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class DAFController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IDAFService _dafService;
        private readonly IProgramaPresupuestarioService _programaPresupuestarioService;
        private readonly IMunicipioServie _municipioService;
        private readonly IAlmacenService _almacenService;

        public DAFController(IMapper mapper, IProgramaPresupuestarioService programaPresupuestarioService, IMunicipioServie municipioService, IAlmacenService almacenService, IDAFService dafService)
        {
            _mapper = mapper;
            _programaPresupuestarioService = programaPresupuestarioService;
            _municipioService = municipioService;
            _almacenService = almacenService;
            _dafService = dafService;
        }

        [HttpGet]
        public async Task<IActionResult> TablaDAF()
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

            listaDTO = await _dafService
                .ListarRequisiciones();

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

        [HttpPost]
        public async Task<IActionResult> Revisar([FromBody] int idRequisicion)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var resultado = await _dafService.RevisarRequisicion(idRequisicion, idUsuario);

            if (!resultado) return BadRequest();
            return Ok();
        }

        /// <summary>IDs en bandeja DAF para notificaciones (sondeo en cliente).</summary>
        [HttpGet]
        public async Task<IActionResult> SnapshotIdsPorTab()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out _))
                return Unauthorized();

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out _))
                return Unauthorized();

            var listaDTO = await _dafService.ListarRequisiciones();
            listaDTO = listaDTO.OrderBy(r => r.FechaModificacion).ThenBy(r => r.IdRequi).ToList();
            var principal = listaDTO.Select(r => r.IdRequi).ToList();

            return Json(new { principal });
        }
    }
}
