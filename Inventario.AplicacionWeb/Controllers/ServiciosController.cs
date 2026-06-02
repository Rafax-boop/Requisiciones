using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.Entity.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Security.Claims;

namespace Inventario.AplicacionWeb.Controllers
{
    public class ServiciosController : Controller
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IMapper _mapper;
        private readonly IRequisicionesService _requisicionesService;
        private readonly IArticulosService _articulosService;
        private readonly IAlmacenService _almacenService;
        private readonly ICatalogoService _catalogoService;
        private readonly IProveedoresService _proveedorService;
        private readonly IWebHostEnvironment _env;
        private readonly IConsolidadaService _consolidadaService;

        public ServiciosController(
            IUsuarioService usuarioService,
            IMapper mapper,
            IRequisicionesService requisicionesService,
            IArticulosService articulosService,
            IAlmacenService almacenService,
            ICatalogoService catalogoService,
            IProveedoresService proveedorService,
            IWebHostEnvironment env,
            IConsolidadaService consolidadaService
        )
        {
            _usuarioService = usuarioService;
            _mapper = mapper;
            _requisicionesService = requisicionesService;
            _articulosService = articulosService;
            _almacenService = almacenService;
            _catalogoService = catalogoService;
            _proveedorService = proveedorService;
            _env = env;
            _consolidadaService = consolidadaService;
        }

        [HttpGet]
        public async Task<IActionResult> TablaRequisicionServicios()
        {
            ViewData["Title"] = "TablaRequisicionServicios";
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out int idDepartamento))
                return RedirectToAction("Login", "Acceso");

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("7"))
            {
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true);
            }

            listaDTO = listaDTO.OrderBy(r => r.FechaModificacion).ToList();

            var actividades = await _catalogoService
                .ObtenerActividades();
            var fuentesFinanciamiento = await _catalogoService.ObtenerFuentesFinanciamiento();
            var municipios = await _catalogoService.ObtenerMunicipios();
            var proveedores = await _proveedorService.ObtenerProveedores();
            var estatus = await _almacenService.ObtenerEstatus();

            var vm = new VMTablaRequisiciones
            {
                Requisiciones = _mapper.Map<List<VMRequisicionMaestra>>(listaDTO),

                ListaActividades = actividades.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = a.Nombre
                }).ToList(),

                ListaFuentesFinanciamiento = fuentesFinanciamiento.Select(f => new SelectListItem
                {
                    Value = f.Clave,
                    Text = f.Nombre
                }).ToList(),

                ListaProveedores = proveedores.Select(p => new SelectListItem
                {
                    Value = p.Id.ToString(),
                    Text = p.Proveedor
                }).ToList(),
                Estatus = estatus
            };

            return View(vm);
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
            ViewBag.UsaFlujoContinuar = true;

            var municipios = await _catalogoService.ObtenerMunicipios();
            ViewBag.ListaMunicipios = municipios.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre
            }).ToList();
            return View(vm);
        }

        [HttpPost]
        public async Task<IActionResult> CrearRequisicion(VMRequiForm modelo, List<IFormFile> Fotos)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            var requiCreada = await _requisicionesService.CrearRequisicion(dto, idUsuario, true);

            if (modelo.TipoServicio == "Servicio Impresion" && Fotos != null && Fotos.Any())
                await _requisicionesService.GuardarFotosRequisicion(requiCreada.IdRequisicion, Fotos, _env.WebRootPath);

            TempData["MensajeExito"] = "Requisición guardada correctamente.";
            TempData["FolioCreado"] = requiCreada.NumRequisicion;
            return RedirectToAction("TablaRequisicionServicios", "Servicios");
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

        [HttpGet]
        public async Task<IActionResult> EditarRequisicion(int id)
        {
            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(id);
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
                Justificacion = dto.Justificacion,
                TipoServicio = dto.TipoServicio,
                FechaServicio = dto.FechaServicio,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada
                }).ToList(),
                FotosExistentes = dto.TipoServicio == "Servicio Impresion"
                    ? (await _requisicionesService.ObtenerFotosConIdRequisicion(id))
                        .Select(f => new VMFotoExistente { IdFoto = f.Id, Ruta = f.Ruta })
                        .ToList()
                    : new List<VMFotoExistente>()
            };

            vm.ObservacionesBitacora = await _requisicionesService.ObtenerObservacionesModificacion(id);

            var municipios = await _catalogoService.ObtenerMunicipios();
            ViewBag.ListaMunicipios = municipios.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre
            }).ToList();
            ViewBag.ModoEdicion = true;
            return View("FormularioRequisicionServicios", vm);
        }

        [HttpPost]
        public async Task<IActionResult> ActualizarRequisicion(VMRequiForm modelo, List<IFormFile> Fotos)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var dto = _mapper.Map<FormularioRequisicionDTO>(modelo);

            bool exito = await _requisicionesService.ActualizarRequisicion(modelo.IdRequiMaestra!.Value, dto, idUsuario);

            if (exito)
            {
                if (modelo.TipoServicio == "Servicio Impresion" && Fotos != null && Fotos.Any())
                    await _requisicionesService.GuardarFotosRequisicion(modelo.IdRequiMaestra!.Value, Fotos, _env.WebRootPath);

                TempData["MensajeExito"] = "Requisición editada correctamente.";
                return RedirectToAction("TablaRequisicionServicios", "Servicios");
            }

            var municipios = await _catalogoService.ObtenerMunicipios();
            ViewBag.ListaMunicipios = municipios.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = m.Nombre
            }).ToList();
            return View("FormularioRequisicionServicios", modelo);
        }

        public async Task<JsonResult> BuscarArticulos(string term)
        {
            var articulos = await _articulosService.BuscarArticulos(term, TipoBusquedaArticulo.Servicio);

            var resultado = articulos.Select(a => new
            {
                id = a.Id,
                text = a.Descripcion
            });

            return Json(resultado);
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerUsuariosServicios()
        {
            var usuarios = await _usuarioService.ListaUsuariosAsignar(7);
            var resultado = usuarios.Select(u => new
            {
                id = u.IdUsuario,
                nombre = u.Usuario
            }).ToList();
            return Json(resultado);
        }

        [HttpGet]
        public async Task<IActionResult> VerParaPdf(int id)
        {
            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var distribucionMunicipios = await _requisicionesService.ObtenerDistribucionMunicipiosPorRequisicion(id);
            var municipios = await _catalogoService.ObtenerMunicipios();

            var vm = new VMRequiForm
            {
                IdRequiMaestra = id,
                NumRequisicion = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                IdDepartamento = dto.IdDepartamento,
                Departamento = dto.Departamento,
                NomResponsableDepartamento = dto.NomResponsableDepartamento,
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                Justificacion = dto.Justificacion,
                TipoServicio = dto.TipoServicio,
                FechaServicio = dto.FechaServicio,
                UsoMaterial = dto.UsoMaterial,
                Hash = dto.Hash,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada,
                    TipoProgramacion = a.TipoProgramacion,
                    Llenado1 = a.Llenado1,
                    Llenado2 = a.Llenado2,
                    Llenado3 = a.Llenado3,
                    Llenado4 = a.Llenado4,
                    Llenado5 = a.Llenado5,
                    Llenado6 = a.Llenado6,
                    Llenado7 = a.Llenado7,
                    Llenado8 = a.Llenado8,
                    Llenado9 = a.Llenado9,
                    Llenado10 = a.Llenado10,
                    Llenado11 = a.Llenado11,
                    Llenado12 = a.Llenado12,
                    Mes = a.Mes,
                    Municipios = distribucionMunicipios.TryGetValue(a.IdRequisicionDetalle, out var muni)
                        ? muni
                        : new List<MunicipioItemDTO>()
                }).ToList()
            };

            ViewBag.ListaMunicipios = municipios;

            return View("RequisicionServiciosParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> HistorialParaPdf(int id)
        {
            var dto = await _requisicionesService.ObtenerRequisicionCompletaPorId(id);
            if (dto == null)
                return NotFound();

            var historial = await _requisicionesService.ObtenerProgresoRequisicion(id);
            var vm = new VMHistorialRequisicionPdf
            {
                IdRequisicion = id,
                TipoRequisicion = "Servicios",
                Folio = dto.NumRequisicion,
                FechaEmision = dto.FechaEmision,
                Departamento = dto.Departamento,
                Responsable = dto.NomResponsableDepartamento,
                IdEstatus = dto.IdEstatus,
                EstatusActual = dto.Estatus,
                Historial = historial.Select(p => new VMHistorialPasoPdf
                {
                    Departamento = p.Dept,
                    Fecha = p.Date,
                    Hora = p.Time,
                    Estado = p.State,
                    Responsable = p.By,
                    Accion = p.Action,
                    Nota = p.Comment
                }).ToList()
            };

            return View("HistorialRequisicionParaPdf", vm);
        }

        [HttpPost]
        public async Task<IActionResult> EliminarFoto(int idFoto)
        {
            var foto = await _requisicionesService.ObtenerFotoPorId(idFoto);
            if (foto != null)
            {
                var rutaFisica = Path.Combine(_env.WebRootPath, foto.Ruta.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(rutaFisica))
                    System.IO.File.Delete(rutaFisica);
            }

            bool exito = await _requisicionesService.EliminarFotoRequisicion(idFoto);
            return Json(new { success = exito });
        }

        [HttpPost]
        public async Task<IActionResult> AceptarExpediente([FromBody] VMRevisarRequisicion modelo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var resultado = await _requisicionesService.AceptarExpediente(modelo.IdRequisicion, idUsuario);
            if (!resultado) return BadRequest();
            return Ok();
        }

        /// <summary>IDs por pestaña para notificaciones (sondeo en cliente).</summary>
        [HttpGet]
        public async Task<IActionResult> SnapshotIdsPorTab()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out int idDepartamento))
                return Unauthorized();

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return Unauthorized();

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("7"))
            {
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionesService
                    .ListarRequisiciones(idDepartamento, true);
            }

            listaDTO = listaDTO
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequi)
                .ToList();

            var principal = listaDTO.Where(r => r.IdEstatus != 7 && r.IdEstatus != 5).Select(r => r.IdRequi).ToList();
            var autorizadas = listaDTO.Where(r => r.IdEstatus == 7).Select(r => r.IdRequi).ToList();
            var rechazadas = listaDTO.Where(r => r.IdEstatus == 5).Select(r => r.IdRequi).ToList();
            var verificadas = listaDTO.Where(r => r.IdEstatus == 16 || r.IdEstatus == 18).Select(r => r.IdRequi).ToList();

            return Json(new { principal, autorizadas, rechazadas, verificadas });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerRequisicionesConArchivos()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
                return Json(new { requisiciones = new List<object>() });

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("7"))
            {
                listaDTO = await _requisicionesService.ObtenerRequisicionesConArchivos(null, true, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionesService.ObtenerRequisicionesConArchivos(null, true);
            }

            var requisiciones = listaDTO.Select(r => new
            {
                idRequi = r.IdRequi,
                numRequi = r.NumRequi,
                fechaEmision = r.FechaEmision.ToString(),
                departamento = r.Departamento,
                responsable = r.Responsable,
                cantidadPartidas = r.CantidadPartidas,
                idEstatus = r.IdEstatus,
                estatus = r.Estatus
            }).ToList();

            return Json(new { requisiciones });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerConsolidadasConArchivos()
        {
            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) || !int.TryParse(idUsuarioClaim, out int idUsuario))
                return Json(new { consolidadas = new List<object>() });

            List<ConsolidadaDTO> lista;
            if (User.IsInRole("7"))
            {
                lista = await _consolidadaService.ObtenerConsolidadasConArchivos(true, idUsuario);
            }
            else
            {
                lista = await _consolidadaService.ObtenerConsolidadasConArchivos(true);
            }

            var consolidadas = lista.Select(c => new
            {
                consolidadaId = c.ConsolidadaID,
                folioConsolidada = c.FolioConsolidada,
                fechaCreacion = c.FechaCreacion,
                cantidadRequis = c.CantidadRequis,
                totalPartidas = c.TotalPartidas,
                departamentos = c.Departamentos,
                idEstatus = c.IdEstatus,
                estatus = c.Estatus
            }).ToList();

            return Json(new { consolidadas });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerTodosLosArchivos(int idRequisicion)
        {
            var archivos = await _requisicionesService.ObtenerTodosLosArchivosDeRequisicion(idRequisicion);
            var resultado = archivos.Select(a => new
            {
                id = a.Id,
                tipo = a.Tipo,
                ruta = a.Ruta,
                fechaSubida = a.FechaSubida?.ToString("dd/MM/yyyy HH:mm") ?? "",
                nombreArchivo = Path.GetFileName(a.Ruta)
            }).ToList();
            return Json(resultado);
        }

        [HttpPost]
        public async Task<IActionResult> SubirDocumentoFirmado([FromForm] int idRequisicion, IFormFile archivo)
        {
            if (!User.IsInRole("1")) return Forbid();
            if (archivo == null || archivo.Length == 0) return BadRequest(new { success = false });
            var ok = await _requisicionesService.SubirDocumentoFirmado(idRequisicion, archivo, _env.WebRootPath);
            return ok ? Ok(new { success = true }) : BadRequest(new { success = false });
        }

        [HttpGet]
        public async Task<JsonResult> ObtenerDocumentoFirmado(int idRequisicion)
        {
            var ruta = await _requisicionesService.ObtenerDocumentoFirmado(idRequisicion);
            if (ruta == null) return Json(new { firmado = false });
            return Json(new { firmado = true, ruta = ruta });
        }
    }
}
