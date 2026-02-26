using Microsoft.AspNetCore.Mvc;

namespace Inventario.AplicacionWeb.Controllers
{
    public class ServiciosController : Controller
    {
        [HttpGet]
        public IActionResult TablaRequisicionServicios()
        {
            ViewData["Title"] = "TablaRequisicionServicios";
            return View();
        }

        [HttpGet]
        public IActionResult FormularioRequisicionServicios()
        {
            ViewData["Title"] = "FormularioRequisicionServicios";
            return View();
        }
    }
}
