using Microsoft.AspNetCore.Mvc;

namespace Inventario.AplicacionWeb.Controllers.Requisiciones
{
    public class Requisiciones : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FormularioRequisiciones()
        {
            return View();
        }
    }
}
