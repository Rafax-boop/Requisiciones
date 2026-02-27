using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;

        public ServiciosController(IUsuarioService usuarioService, IMapper mapper)
        {
            _usuarioService = usuarioService;
            _mapper = mapper;
        }

        [HttpGet]
        public IActionResult TablaRequisicionServicios()
        {
            ViewData["Title"] = "TablaRequisicionServicios";
            return View();
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
    }
}
