using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    [Authorize]
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

        [HttpGet]
        public async Task<IActionResult> FormularioRequisiciones([FromQuery] string? tipo = "general")
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            if (tipo != "mensual")
                tipo = "general";

            ViewBag.TipoRequisicion = tipo;

            var dto = await _usuarioService.ObtenerDatosDepartamento(idUsuario);

            var vm = _mapper.Map<VMRequiForm>(dto);

            return View(vm);
        }

        public async Task<IActionResult> TablaRequisiciones()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) || !int.TryParse(idDeptoClaim, out int idDepartamento))
                return RedirectToAction("Login", "Acceso");

            var listaDTO = await _requisicionService.ListarRequisiciones(idDepartamento);
            var viewModel = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO);
            
            return View(viewModel);
        }

        public async Task<JsonResult> ObtenerDetalles(int idMaestro)
        {
            var detalles = await _requisicionService.ObtenerDetallePorIdMaestro(idMaestro);
            return Json(detalles);
        }

        [HttpGet]
        public async Task<IActionResult> VerParaPdf(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var vm = new VMRequiForm
            {
                NumRequisicion = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                NomDirector = dto.NomDirector,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                UsoEspecifico = dto.UsoEspecifico,
                Justificacion = dto.Justificacion,
                CuentaProgramaPresupuestario = dto.CuentaProgramaPresupuestario,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada
                }).ToList()
            };

            return View("RequisicionParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> EditarRequisicion(int id)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                UsoEspecifico = dto.UsoEspecifico,
                Justificacion = dto.Justificacion,
                CuentaProgramaPresupuestario = dto.CuentaProgramaPresupuestario,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada
                }).ToList()
            };

            ViewBag.ModoEdicion = true;
            return View("FormularioRequisiciones", vm);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionService.ActualizarRequisicion(modelo.IdRequiMaestra!.Value, dto, idUsuario);

            if (exito)
            {
                TempData["MensajeExito"] = "Requisición editada correctamente.";
                return RedirectToAction("TablaRequisiciones", "Requisicion");
            }

            ViewBag.ModoEdicion = true;
            return View("FormularioRequisiciones", modelo);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionService.CrearRequisicion(dto, idUsuario);

            if (exito)
            {
                TempData["MensajeExito"] = "Requisición guardada correctamente.";
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
