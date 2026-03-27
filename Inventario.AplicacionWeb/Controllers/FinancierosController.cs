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
    public class FinancierosController : Controller
    {
        private readonly IMapper _mapper;
        private readonly IFinancierosService _financierosService;
        private readonly IUsuarioService _usuarioService;

        public FinancierosController(IMapper mapper, IFinancierosService financierosService, IUsuarioService usuarioService)
        {
            _mapper = mapper;
            _financierosService = financierosService;
            _usuarioService = usuarioService;
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

            var vm = new VMTablaRequisiciones
            {
                Requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO)
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
        public async Task<IActionResult> Atender([FromBody] VMAtenderRequisicion modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var dto = _mapper.Map<AtenderRequiDTO>(modelo);

            var resultado = await _financierosService.AtenderRequisicion(dto, idUsuario);

            if (!resultado) return BadRequest();
            return Ok();
        }
    }
}
