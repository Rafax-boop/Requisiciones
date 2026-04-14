using AutoMapper;
using Inventario.AplicacionWeb.Models.ViewModels;
using Inventario.BLL.DTO;
using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.Entity.Enums;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
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
        private readonly IMunicipioServie _municipioService;
        private readonly IProgramaPresupuestarioService _programaPresupuestarioService;
        private readonly IAlmacenService _almacenService;
        private readonly IProveedoresService _proveedoresService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public RequisicionController(
            IRequisicionesService requisicionesService,
            IMapper mapper, IArticulosService articulosService,
            IUsuarioService usuarioService,
            IMunicipioServie municipioService,
            IProgramaPresupuestarioService programaPresupuestarioService,
            IAlmacenService almacenService,
            IProveedoresService proveedoresService,
            IWebHostEnvironment webHostEnvironment
        )
        {
            _requisicionService = requisicionesService;
            _mapper = mapper;
            _articulosService = articulosService;
            _usuarioService = usuarioService;
            _programaPresupuestarioService = programaPresupuestarioService;
            _municipioService = municipioService;
            _almacenService = almacenService;
            _proveedoresService = proveedoresService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> FormularioRequisiciones([FromQuery] string? tipo = "general")
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            if (tipo != "mensual" && tipo != "servicios")
                tipo = "general";

            ViewBag.TipoRequisicion = tipo;

            var dto = await _usuarioService.ObtenerDatosDepartamento(idUsuario);

            var vm = _mapper.Map<VMRequiForm>(dto);

            ViewBag.UsaFlujoContinuar = true;

            return View(vm);
        }

        public async Task<IActionResult> TablaRequisiciones()
        {
            var idDeptoClaim = User.FindFirst("IdDepartamento")?.Value;
            if (string.IsNullOrEmpty(idDeptoClaim) ||
                !int.TryParse(idDeptoClaim, out int idDepartamento))
                return RedirectToAction("Login", "Acceso");

            var idUsuarioClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idUsuarioClaim) ||
                !int.TryParse(idUsuarioClaim, out int idUsuario))
                return RedirectToAction("Login", "Acceso");

            List<RequisicionMaestraDTO> listaDTO;

            if (User.IsInRole("3"))
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false);
            }

            listaDTO = listaDTO
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequi)
                .ToList();

            var actividades = await _programaPresupuestarioService
                .ObtenerActividades();

            var municipios = await _municipioService.ObtenerMunicipios();
            var proveedores = await _proveedoresService.ObtenerProveedores();
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
        public async Task<JsonResult> ObtenerUsuariosMateriales()
        {
            var usuarios = await _usuarioService.ListaUsuariosAsignar(3);
            var resultado = usuarios.Select(u => new
            {
                id = u.IdUsuario,
                nombre = u.Usuario
            }).ToList();
            return Json(resultado);
        }

        public async Task<JsonResult> ObtenerDetalles(int idMaestro)
        {
            var detalles = await _requisicionService.ObtenerDetallePorIdMaestro(idMaestro);
            return Json(detalles);
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerProgresoRequisicion(int idRequisicion)
        {
            var pasos = await _requisicionService.ObtenerProgresoRequisicion(idRequisicion);
            return Json(pasos);
        }

        [HttpGet]
        public async Task<IActionResult> VerParaPdf(int id)
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
                CargoResponsableDepartamento = dto.CargoResponsableDepartamento,
                NomDirector = dto.NomDirector,
                CargoDirector = dto.CargoDirector,
                Correo = dto.Correo,
                Telefono = dto.Telefono,
                LugarEntrega = dto.LugarEntrega,
                UsoEspecifico = dto.UsoEspecifico,
                Justificacion = dto.Justificacion,
                CuentaProgramaPresupuestario = dto.CuentaProgramaPresupuestario,
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
                    Mes = a.Mes
                }).ToList()
            };

            return View("RequisicionParaPdf", vm);
        }

        [HttpGet]
        public async Task<IActionResult> DescargarCuadroComparativo(int idRequisicion)
        {
            var dto = await _requisicionService.ObtenerRequisicionCompletaPorId(idRequisicion);
            if (dto == null)
                return NotFound();

            var filas = dto.Articulos?
                .Select(a =>
                {
                    var cantidadTexto = a.Cantidad.HasValue
                        ? decimal.Truncate(a.Cantidad.Value) == a.Cantidad.Value
                            ? ((int)a.Cantidad.Value).ToString(CultureInfo.InvariantCulture)
                            : a.Cantidad.Value.ToString("0.##", CultureInfo.InvariantCulture)
                        : "";

                    return new CuadroComparativoFilaPdf
                    {
                        Partida = a.NumPartida?.ToString() ?? "",
                        Descripcion = a.Descripcion ?? a.DescripcionDetallada ?? "",
                        Cantidad = cantidadTexto,
                        Unidad = a.UnidadMedida ?? ""
                    };
                })
                .ToList() ?? new List<CuadroComparativoFilaPdf>();

            var bytes = GenerarCuadroComparativoPdf(
                requisicion: dto.NumRequisicion ?? $"REQ-{idRequisicion}",
                fecha: dto.FechaEmision?.ToDateTime(TimeOnly.MinValue),
                departamento: dto.Departamento ?? "",
                justificacion: dto.Justificacion ?? "",
                filas: filas);

            var nombreArchivo = $"CuadroComparativo_{(dto.NumRequisicion ?? idRequisicion.ToString()).Replace("/", "-")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
            return File(bytes, "application/pdf", nombreArchivo);
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
                UsoMaterial = dto.UsoMaterial,
                Articulos = dto.Articulos.Select(a => new ItemRequiVM
                {
                    IdArticulo = a.IdArticulo,
                    Cog = a.NumPartida,
                    ClaveMaterial = a.ClaveMaterial,
                    Cantidad = a.Cantidad,
                    UnidadMedida = a.UnidadMedida,
                    Descripcion = a.Descripcion,
                    DescripcionDetallada = a.DescripcionDetallada
                }).ToList()
            };

            vm.ObservacionesBitacora = await _requisicionService.ObtenerObservacionesModificacion(id);

            ViewBag.ModoEdicion = true;
            return View("FormularioRequisiciones", vm);
        }

        private byte[] GenerarCuadroComparativoPdf(
            string requisicion,
            DateTime? fecha,
            string departamento,
            string justificacion,
            List<CuadroComparativoFilaPdf> filas)
        {
            var ms = new MemoryStream();
            using var writer = new PdfWriter(ms);
            using var pdf = new PdfDocument(writer);
            using var doc = new Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(18f, 16f, 18f, 16f);

            var fontBold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var fontRegular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            var fondoEncabezado = new DeviceRgb(248, 250, 252);
            var textoPrincipal = new DeviceRgb(26, 26, 26);
            var textoSecundario = new DeviceRgb(100, 116, 139);
            var textoEncabezado = new DeviceRgb(71, 85, 105);
            var textoInstitucional = new DeviceRgb(45, 45, 45);
            var rosaAcento = new DeviceRgb(255, 45, 111);
            var borde = new SolidBorder(new DeviceRgb(226, 232, 240), 0.85f);

            Cell CellHead(string text, int colspan = 1, int rowspan = 1, TextAlignment align = TextAlignment.LEFT)
                => new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetBackgroundColor(fondoEncabezado)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text).SetFont(fontBold).SetFontSize(6.6f).SetFontColor(textoEncabezado));

            Cell CellBody(string text, int colspan = 1, int rowspan = 1, TextAlignment align = TextAlignment.LEFT, float minHeight = 0f)
            {
                var c = new Cell(rowspan, colspan)
                    .SetBorder(borde)
                    .SetPadding(4f)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(align)
                    .Add(new Paragraph(text).SetFont(fontRegular).SetFontSize(6.8f).SetFontColor(textoPrincipal));
                if (minHeight > 0f)
                    c.SetMinHeight(minHeight);
                return c;
            }

            var header = new Table(UnitValue.CreatePercentArray(new float[] { 18f, 64f, 18f })).UseAllAvailableWidth();
            header.SetBorder(borde);

            var logoIzq = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoIzq = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "img", "corazon.png");
            if (System.IO.File.Exists(rutaLogoIzq))
            {
                var img = new Image(ImageDataFactory.Create(rutaLogoIzq)).ScaleToFit(90f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                logoIzq.Add(img);
            }
            else
            {
                logoIzq.Add(new Paragraph("PUEBLA").SetFont(fontBold).SetFontSize(10f).SetTextAlignment(TextAlignment.CENTER));
            }
            header.AddCell(logoIzq);

            var centro = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetPadding(6f);
            centro.Add(new Paragraph("SISTEMA PARA EL DESARROLLO INTEGRAL DE LA FAMILIA DEL ESTADO DE PUEBLA")
                .SetFont(fontBold).SetFontSize(7.2f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DIRECCION DE ADMINISTRACION Y FINANZAS")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("DEPARTAMENTO DE RECURSOS MATERIALES Y SERVICIOS GENERALES")
                .SetFont(fontBold).SetFontSize(6.8f).SetFontColor(textoInstitucional));
            centro.Add(new Paragraph("CUADRO COMPARATIVO")
                .SetFont(fontBold).SetFontSize(10f).SetFontColor(rosaAcento).SetMarginTop(4f));
            header.AddCell(centro);

            var logoDer = new Cell().SetBorder(borde).SetTextAlignment(TextAlignment.CENTER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetPadding(6f);
            var rutaLogoDer = System.IO.Path.Combine(_webHostEnvironment.WebRootPath, "img", "familias-dif-rosa.png");
            if (System.IO.File.Exists(rutaLogoDer))
            {
                var img = new Image(ImageDataFactory.Create(rutaLogoDer)).ScaleToFit(95f, 42f).SetHorizontalAlignment(HorizontalAlignment.CENTER);
                logoDer.Add(img);
            }
            else
            {
                logoDer.Add(new Paragraph("Familias").SetFont(fontBold).SetFontSize(10f).SetTextAlignment(TextAlignment.CENTER));
            }
            header.AddCell(logoDer);
            doc.Add(header);
            doc.Add(new Paragraph(" ").SetMargin(2f));

            var fechaTxt = fecha.HasValue ? fecha.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "";
            var info = new Table(UnitValue.CreatePercentArray(new float[] { 20f, 58f, 10f, 12f })).UseAllAvailableWidth();
            info.AddCell(CellHead("SECCION DE ADQUISICIONES", 4));
            info.AddCell(CellHead("TIPO DE PROCEDIMIENTO:"));
            info.AddCell(CellBody("PROCEDIMIENTO DE ADJUDICACION (PENDIENTE DE CAPTURA)", 3));
            info.AddCell(CellHead("CRITERIO DE ADJUDICACION:"));
            info.AddCell(CellBody("SELECCION AL PROVEEDOR QUE CUMPLA CON REQUISITOS LEGALES Y OFERTE EL PRECIO MAS BAJO.", 3));
            info.AddCell(CellHead("REQUISICION"));
            info.AddCell(CellBody(requisicion, 1, 1, TextAlignment.CENTER));
            info.AddCell(CellHead("FECHA:", 1, 1, TextAlignment.CENTER));
            info.AddCell(CellBody(fechaTxt, 1, 1, TextAlignment.CENTER));
            info.AddCell(CellHead("JUSTIFICACION:"));
            info.AddCell(CellBody(string.IsNullOrWhiteSpace(justificacion) ? " " : justificacion, 3));
            info.AddCell(CellHead("AREAS SOLICITANTES:"));
            info.AddCell(CellBody(string.IsNullOrWhiteSpace(departamento) ? " " : departamento, 3));
            doc.Add(info);
            doc.Add(new Paragraph(" ").SetMargin(2f));

            var tabla = new Table(UnitValue.CreatePercentArray(new float[] { 7f, 31f, 7f, 7f, 8f, 8f, 8f, 8f, 8f, 8f })).UseAllAvailableWidth();
            tabla.AddHeaderCell(CellHead("PARTIDA", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("DESCRIPCION", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("CANTIDAD", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("U.M.", 1, 2, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 1", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 2", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("PROVEEDOR 3", 2, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("P/UNITARIO", 1, 1, TextAlignment.CENTER));
            tabla.AddHeaderCell(CellHead("TOTAL", 1, 1, TextAlignment.CENTER));

            var totalFilas = Math.Max(2, filas.Count);
            for (var i = 0; i < totalFilas; i++)
            {
                var fila = i < filas.Count ? filas[i] : new CuadroComparativoFilaPdf();
                var fondoFila = i % 2 == 1 ? fondoEncabezado : ColorConstants.WHITE;
                var c1 = CellBody(string.IsNullOrWhiteSpace(fila.Partida) ? (i + 1).ToString() : fila.Partida, align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c2 = CellBody(fila.Descripcion ?? "", minHeight: 17f).SetBackgroundColor(fondoFila);
                var c3 = CellBody(fila.Cantidad ?? "", align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c4 = CellBody(fila.Unidad ?? "", align: TextAlignment.CENTER, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c5 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c6 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c7 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c8 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c9 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);
                var c10 = CellBody("", align: TextAlignment.RIGHT, minHeight: 17f).SetBackgroundColor(fondoFila);

                tabla.AddCell(c1);
                tabla.AddCell(c2);
                tabla.AddCell(c3);
                tabla.AddCell(c4);
                tabla.AddCell(c5);
                tabla.AddCell(c6);
                tabla.AddCell(c7);
                tabla.AddCell(c8);
                tabla.AddCell(c9);
                tabla.AddCell(c10);
            }

            foreach (var etiqueta in new[] { "SUMA", "IVA", "TOTAL" })
            {
                tabla.AddCell(CellBody("", 4));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
                tabla.AddCell(CellHead(etiqueta, 1, 1, TextAlignment.CENTER));
                tabla.AddCell(CellBody("$", align: TextAlignment.CENTER));
            }

            tabla.AddCell(CellHead("CONDICIONES DE PAGO", 4, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            tabla.AddCell(CellBody("CREDITO 30 DIAS", 2, 1, TextAlignment.CENTER));
            doc.Add(tabla);
            doc.Add(new Paragraph(" ").SetMargin(3f));

            var proveedor = new Table(UnitValue.CreatePercentArray(new float[] { 44f, 16f, 10f, 10f, 10f, 10f })).UseAllAvailableWidth();
            proveedor.AddCell(CellHead("PROVEEDOR(ES) SELECCIONADO(S)"));
            proveedor.AddCell(CellBody(""));
            proveedor.AddCell(CellHead("SUMA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("IVA", align: TextAlignment.CENTER));
            proveedor.AddCell(CellHead("SUBTOTAL", align: TextAlignment.CENTER));
            proveedor.AddCell(CellBody("$", align: TextAlignment.CENTER));
            doc.Add(proveedor);
            doc.Add(new Paragraph(" ").SetMargin(8f));

            var tblFirmas = new Table(UnitValue.CreatePointArray(new float[] { 173f, 173f, 174f }))
                .UseAllAvailableWidth();
            var firmantes = new (string Titulo, string Nombre)[]
            {
                ("ASIGNACIÓN PRESUPUESTAL", "JOEL MARTÍNEZ PÉREZ\nJEFE DEL DEPARTAMENTO DE RECURSOS\nFINANCIEROS"),
                ("Vo.Bo.", "C. MARCOS MATAMOROS MORENO\nDIRECTOR DE ADMINISTRACIÓN Y FINANZAS"),
                ("AUTORIZÓ", "C. CIRO MIGUEL JUÁREZ PALACIOS\nTITULAR DE LA UNIDAD DE PLANEACIÓN,\nADMINISTRACIÓN Y FINANZAS"),
            };

            foreach (var (titulo, nombre) in firmantes)
            {
                tblFirmas.AddCell(new Cell()
                    .SetMinHeight(78f)
                    .SetBackgroundColor(ColorConstants.WHITE)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetVerticalAlignment(VerticalAlignment.TOP)
                    .SetPaddingTop(10f)
                    .SetPaddingBottom(10f)
                    .SetPaddingLeft(6f)
                    .SetPaddingRight(6f)
                    .SetBorder(borde)
                    .Add(new Paragraph(titulo).SetFont(fontBold).SetFontSize(5.75f)
                        .SetFontColor(textoEncabezado).SetMarginBottom(10f))
                    .Add(new Paragraph(" ").SetFontSize(22f))
                    .Add(new Paragraph("_________________________________________").SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoPrincipal).SetMarginBottom(6f))
                    .Add(new Paragraph(nombre.Replace("\n", " ")).SetFont(fontRegular).SetFontSize(5f)
                        .SetFontColor(textoSecundario)
                        .SetTextAlignment(TextAlignment.CENTER)));
            }
            doc.Add(tblFirmas);

            doc.Close();
            return ms.ToArray();
        }

        private sealed class CuadroComparativoFilaPdf
        {
            public string Partida { get; set; } = "";
            public string Descripcion { get; set; } = "";
            public string Cantidad { get; set; } = "";
            public string Unidad { get; set; } = "";
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

            var requiCreada = await _requisicionService.CrearRequisicion(dto, idUsuario, false);

            TempData["MensajeExito"] = "Requisición guardada correctamente.";
            TempData["FolioCreado"] = requiCreada.NumRequisicion;
            return RedirectToAction("TablaRequisiciones", "Requisicion");
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

        public async Task<JsonResult> BuscarArticulos(string term, string tipo = "normal")
        {
            TipoBusquedaArticulo tipoBusqueda = tipo switch
            {
                "mensual" => TipoBusquedaArticulo.Mensual,
                _ => TipoBusquedaArticulo.Normal
            };

            var articulos = await _articulosService.BuscarArticulos(term, tipoBusqueda);

            var resultado = articulos.Select(a => new
            {
                id = a.Id,
                text = a.Descripcion
            });

            return Json(resultado);
        }

        [HttpGet]
        public async Task<JsonResult> BuscarCogs(string term)
        {
            var cogs = await _articulosService.BuscarCogs(term);
            return Json(cogs.Select(c => new { id = c, text = c.ToString() }));
        }

        [HttpPost]
        public async Task<JsonResult> AsignarRequisicion(int idRequi, int idUsuario)
        {
            int idUsuarioLog = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.AsignarRequisicion(idRequi, idUsuarioLog, idUsuario);
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

            var resultado = await _requisicionService.AtenderRequisicion(dto, idUsuario);

            if (!resultado) return BadRequest();
            return Ok();
        }

        [HttpPost]
        public async Task<JsonResult> EnviarAAlmacen(int idRequi)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.EnviarAAlmacen(idRequi, idUsuario);
                return Json(new { success = resultado });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = ex.Message, detalle = ex.InnerException?.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> EnviarAModificacion(int idRequi, string observaciones)
        {
            try
            {
                var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                var resultado = await _requisicionService.EnviarAModificacion(idRequi, idUsuario, observaciones);

                if (!resultado)
                    return Json(new { success = false, mensaje = "No se encontró la requisición." });

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, mensaje = "Error interno.", detalle = ex.Message });
            }
        }

        [HttpPost]
        public async Task<JsonResult> RechazarRequisicion(int idRequi, string motivo)
        {
            int idUsuario = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                var resultado = await _requisicionService.RechazarRequisicion(idRequi, idUsuario, motivo);
                return Json(new { success = resultado });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SubirArchivosAtencion(
            int IdRequisicion,
            //List<IFormFile>? Cotizaciones,
            List<IFormFile>? CuadroComparativo,
            List<IFormFile> anexos)
        {
            var webRootPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            await _requisicionService.GuardarArchivosAtencion(
                IdRequisicion,
                //Cotizaciones ?? new List<IFormFile>(),
                CuadroComparativo ?? new List<IFormFile>(),
                anexos,
                webRootPath
            );
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> SubirDocumentoProveedor(
    [FromForm] int idRequisicion,
    [FromForm] string tipoDocumento,
    IFormFile archivo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.SubirDocumentoProveedor(
                idRequisicion, tipoDocumento, archivo,
                _webHostEnvironment.WebRootPath, idUsuario);

            return ok ? Ok(new { success = true })
                      : BadRequest(new { success = false });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerDocumentosProveedor(int idRequisicion)
        {
            var docs = await _requisicionService.ObtenerDocumentosProveedor(idRequisicion);
            return Ok(docs);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarAFinancierosConDocs([FromForm] int idRequisicion)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.EnviarAFinancierosConDocs(idRequisicion, idUsuario);
            return Ok(new { success = ok });
        }

        [HttpPost]
        public async Task<IActionResult> RebotarDocumentos([FromBody] RebotarDocumentosDTO modelo)
        {
            var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var ok = await _requisicionService.RebotarDocumentos(
                modelo.IdRequisicion, modelo.Observaciones,
                modelo.DocumentosObservados, idUsuario);
            return Ok(new { success = ok });
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

            if (User.IsInRole("3"))
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false, idUsuario);
            }
            else
            {
                listaDTO = await _requisicionService
                    .ListarRequisiciones(idDepartamento, false);
            }

            listaDTO = listaDTO
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequi)
                .ToList();

            var principal = listaDTO.Where(r => r.IdEstatus != 4 && r.IdEstatus != 5).Select(r => r.IdRequi).ToList();
            var autorizadas = listaDTO.Where(r => r.IdEstatus == 4).Select(r => r.IdRequi).ToList();
            var rechazadas = listaDTO.Where(r => r.IdEstatus == 5).Select(r => r.IdRequi).ToList();
            var verificadas = listaDTO.Where(r => r.IdEstatus == 16 || r.IdEstatus == 18).Select(r => r.IdRequi).ToList();

            return Json(new { principal, autorizadas, rechazadas, verificadas });
        }

        [HttpPost]
        public async Task<IActionResult> GuardarCotizaciones([FromBody] GuardarCotizacionesRequest modelo)
        {
            var ok = await _proveedoresService.GuardarCotizaciones(modelo.IdRequisicion, modelo.Cotizaciones);
            return Ok(new { success = ok });
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerCotizaciones(int idRequisicion)
        {
            var cotizaciones = await _proveedoresService.ObtenerCotizaciones(idRequisicion);
            return Ok(cotizaciones);
        }                                                                                                                         
    }
}
