using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class AccesoController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        public AccesoController(IUsuarioService usuarioService)
        {
           _usuarioService = usuarioService;
        }
        public IActionResult Login()
        {
            ClaimsPrincipal User = HttpContext.User;
            if(User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(VMUsuarioLogin modelo)
        {
            try
            {
                var usuarioEncontrado = await _usuarioService.ObtenerPorCredenciales(modelo.Usuario, modelo.Password);
                if (usuarioEncontrado == null)
                {
                    ViewBag.Error = "Usuario o contraseña incorrectos";
                    return View();
                }
                
                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuarioEncontrado.Usuario),
                    new Claim(ClaimTypes.NameIdentifier, usuarioEncontrado.IdUsuario.ToString()),
                    new Claim("IdDepartamento", usuarioEncontrado.Area.ToString()),
                    new Claim(ClaimTypes.Role, usuarioEncontrado.IdRol.ToString())
                };

                ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                AuthenticationProperties properties = new AuthenticationProperties
                {
                    AllowRefresh = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    properties
                );

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Ocurrió un error al procesar la solicitud";
                return View();
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Acceso");
        }
    }
}
