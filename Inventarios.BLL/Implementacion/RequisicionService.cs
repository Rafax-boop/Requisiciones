using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Inventario.BLL.Implementacion
{
    public class RequisicionService : IRequisicionesService
    {
        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblRequisicionDetalle> _repositoryRequisicionDetalle;
        private readonly IGenericRepository<TblBitacoraEstatus> _repositoryBitacora;
        private readonly IGenericRepository<TblDepartamento> _repositoryDepartamento;
        private readonly IGenericRepository<TblEstatus> _repositoryEstatus;
        private readonly IGenericRepository<TblRegistroDiseno> _repositoryDisenos;
        private readonly IGenericRepository<TblArticulosProgramado> _repositoryProgramacion;
        private readonly IUnitOfWork _unitOfWork;

        public RequisicionService(
            IRequisicionRepository repositoryRequisicion,
            IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle,
            IGenericRepository<TblBitacoraEstatus> repositoryBitacora,
            IGenericRepository<TblDepartamento> repositoryDepartamento,
            IGenericRepository<TblEstatus> repositoryEstatus,
            IGenericRepository<TblRegistroDiseno> repositoryDisenos,
            IGenericRepository<TblArticulosProgramado> repositoryProgramacion,
            IUnitOfWork unitOfWork

        )
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryRequisicionDetalle = repositoryRequisicionDetalle;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDepartamento = repositoryDepartamento;
            _repositoryEstatus = repositoryEstatus;
            _repositoryDisenos = repositoryDisenos;
            _repositoryProgramacion = repositoryProgramacion;
            _unitOfWork = unitOfWork;
        }

        public async Task<TblRequisicion> CrearRequisicion(FormularioRequisicionDTO modelo, int idUsuario, bool servicio)
        {
            var requisicion = new TblRequisicion
            {
                FechaEmision = DateOnly.FromDateTime(DateTime.Now),
                IdDepartamento = modelo.IdDepartamento,
                NomResponsableDepartamento = modelo.NomResponsableDepartamento,
                NombreDirector = modelo.NomDirector,
                Correo = modelo.Correo,
                Telefono = modelo.Telefono,
                LugarEntrega = modelo.LugarEntrega,
                UsoEspecifico = !servicio ? modelo.UsoEspecifico.ToUpper() : null,
                Justificacion = modelo.Justificacion.ToUpper(),
                CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario,
                Donativo = !servicio ? modelo.UsoMaterial : null,
                IdUsuario = idUsuario,
                IdEstatus = 1,
                FechaModificacion = DateTime.Now,
                Hash = GenerarSelloDigital(),
                RequiServicio = servicio,
                TipoServicio = servicio ? modelo.TipoServicio : null,
                FechaServicio = servicio ? modelo.FechaServicio : null
            };
            var requiCreada = await _repositoryRequisicion.CrearConFolio(requisicion);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requiCreada.IdRequisicion,
                IdEstatus = requiCreada.IdEstatus,
                FechaEstatus = requiCreada.FechaModificacion,
                Observacion = "FormularioRequisiciones",
                IdUsuario = idUsuario
            };
            var bitacoraCreada = await _repositoryBitacora.Crear(bitacora);

            var listaArticulos = new List<TblRequisicionDetalle>();

            foreach (var item in modelo.Articulos)
            {
                var detalle = new TblRequisicionDetalle
                {
                    IdRequisicion = requiCreada.IdRequisicion,
                    NumPartida = item.Cog,
                    IdArticulo = item.IdArticulo,
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                    Descripcion = item.Descripcion,
                    DescripcionDetallada = item.DescripcionDetallada.ToUpper(),
                    FechaRegistro = DateTime.Now.Date
                };
                listaArticulos.Add(detalle);
            }

            await _repositoryRequisicionDetalle.CrearRango(listaArticulos);

            // Guardar programación si algún artículo la trae
            var listaProgramacion = new List<TblArticulosProgramado>();

            for (int i = 0; i < modelo.Articulos.Count; i++)
            {
                var item = modelo.Articulos[i];

                if (string.IsNullOrEmpty(item.TipoProgramacion)) continue;

                var detalle = listaArticulos[i]; // ← mismo índice, siempre correcto

                var prog = new TblArticulosProgramado
                {
                    IdRequisicion = requiCreada.IdRequisicion,
                    IdRequisicionDetalle = detalle.IdRequisicionDetalle,
                    IdArticulo = item.IdArticulo,
                    TipoProgramacion = item.TipoProgramacion,
                    Llenado1 = item.Llenado1,
                    Llenado2 = item.Llenado2,
                    Llenado3 = item.Llenado3,
                    Llenado4 = item.Llenado4,
                    Llenado5 = item.TipoProgramacion == "anual" ? item.Llenado5 : null,
                    Llenado6 = item.TipoProgramacion == "anual" ? item.Llenado6 : null,
                    Llenado7 = item.TipoProgramacion == "anual" ? item.Llenado7 : null,
                    Llenado8 = item.TipoProgramacion == "anual" ? item.Llenado8 : null,
                    Llenado9 = item.TipoProgramacion == "anual" ? item.Llenado9 : null,
                    Llenado10 = item.TipoProgramacion == "anual" ? item.Llenado10 : null,
                    Llenado11 = item.TipoProgramacion == "anual" ? item.Llenado11 : null,
                    Llenado12 = item.TipoProgramacion == "anual" ? item.Llenado12 : null,
                };
                listaProgramacion.Add(prog);
            }

            if (listaProgramacion.Any())
                await _repositoryProgramacion.CrearRango(listaProgramacion);

            return requiCreada;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idDepartamento, bool servicio, int? idUsuarioMat = null)
        {
            IQueryable<TblRequisicion> query;

            if (idUsuarioMat.HasValue)
            {
                query = await _repositoryRequisicion.Consultar(r => r.IdUsuarioMat == idUsuarioMat.Value);
            }
            else if (idDepartamento < 110)
            {
                query = await _repositoryRequisicion.Consultar(r => r.IdDepartamento == idDepartamento);
            }
            else
            {
                query = await _repositoryRequisicion.Consultar();
            }

            query = query.Where(r => r.RequiServicio == servicio);

            var resultado = await query
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    FechaModificacion = r.FechaModificacion,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
                    IdEstatus = r.IdEstatus ?? 0,
                    Estatus = r.IdEstatusNavigation.NombreEstatus,
                    CantidadPartidas = r.TblRequisicionDetalles.Count,
                    DiasAsignado = r.TblBitacoraEstatuses
                        .Where(b => b.IdEstatus == 2)
                        .OrderByDescending(b => b.FechaEstatus)
                        .Select(b => (DateTime.Now - (b.FechaEstatus ?? DateTime.Now)).Days)
                        .FirstOrDefault(),
                    NombreAsignado = r.IdUsuarioMatNavigation != null ? r.IdUsuarioMatNavigation.Usuario : null
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesAutorizadas()
        {
            IQueryable<TblRequisicion> query = await _repositoryRequisicion.Consultar(r => r.IdEstatus == 4);

            var resultado = await query
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
                    Estatus = r.IdEstatusNavigation.NombreEstatus,
                    CantidadPartidas = r.TblRequisicionDetalles.Count
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro)
        {
            var queryMaestra = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idMaestro);
            var maestra = await queryMaestra.FirstOrDefaultAsync();

            var query = await _repositoryRequisicionDetalle.Consultar(r => r.IdRequisicion == idMaestro);
            var lista = await query
                .Select(r => new DetalleArticuloDTO
                {
                    IdRequisicionDetalle = r.IdRequisicionDetalle,
                    NumPartida = r.NumPartida,
                    IdArticulo = r.IdArticulo,
                    Cantidad = r.Cantidad,
                    UnidadMedida = r.UnidadMedida,
                    Descripcion = r.Descripcion,
                    DescripcionDetallada = r.DescripcionDetallada
                })
                .ToListAsync();

            List<string> fotos = new();
            if (maestra?.TipoServicio == "Servicio Impresion")
            {
                var queryFotos = await _repositoryDisenos.Consultar(f => f.IdRequisicion == idMaestro && f.Tipo == "diseno");
                fotos = await queryFotos.Select(f => f.Ruta).ToListAsync();
            }

            var queryCotizaciones = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "cotizacion");
            var cotizaciones = await queryCotizaciones
                .Select(f => new ArchivoAtencionDTO
                {
                    Ruta = f.Ruta,
                    NombreArchivo = Path.GetFileName(f.Ruta)
                }).ToListAsync();

            // Obtener última observación de atención (estatus 13)
            var queryBitacora = await _repositoryBitacora.Consultar(b =>
                b.IdRequisicion == idMaestro && b.IdEstatus == 13);

            var observacion = await queryBitacora
                .OrderByDescending(b => b.FechaEstatus)
                .Select(b => b.Observacion)
                .FirstOrDefaultAsync();

            var queryCuadro = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "cuadro_comparativo");
            var cuadro = await queryCuadro
                .Select(f => new ArchivoAtencionDTO
                {
                    Ruta = f.Ruta,
                    NombreArchivo = Path.GetFileName(f.Ruta)
                }).ToListAsync();

            return new DetallesRequiDTO
            {
                Donativo = maestra?.Donativo ?? false,
                TipoServicio = maestra?.TipoServicio,
                Fotos = fotos,
                IdPp = maestra?.IdPp,
                Ff = maestra?.Ff,
                TipoPrograma = maestra?.TipoPrograma,
                ClaveRegion = maestra?.ClaveRegion,
                Articulos = lista,
                Cotizaciones = cotizaciones,
                CuadroComparativo = cuadro,
                Observaciones = observacion
            };
        }

        public async Task<RequisicionCompletaDTO?> ObtenerRequisicionCompletaPorId(int idRequisicion)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return null;

            string? nombreDepartamento = null;
            if (requisicion.IdDepartamento.HasValue)
            {
                var departamento = await _repositoryDepartamento.Obtener(d => d.IdDepartamento == requisicion.IdDepartamento.Value);
                nombreDepartamento = departamento?.NombreDepartamento;
            }

            var queryDetalles = await _repositoryRequisicionDetalle.Consultar(r => r.IdRequisicion == idRequisicion);
            var articulos = await queryDetalles
                .Select(r => new DetalleArticuloDTO
                {
                    IdRequisicionDetalle = r.IdRequisicionDetalle,
                    NumPartida = r.NumPartida,
                    ClaveMaterial = r.IdArticuloNavigation.ClaveMaterial,
                    IdArticulo = r.IdArticulo,
                    Cantidad = r.Cantidad,
                    UnidadMedida = r.UnidadMedida,
                    Descripcion = r.Descripcion,
                    DescripcionDetallada = r.DescripcionDetallada
                })
                .ToListAsync();

            return new RequisicionCompletaDTO
            {
                NumRequisicion = requisicion.NumRequisicion,
                FechaEmision = requisicion.FechaEmision,
                IdDepartamento = requisicion.IdDepartamento,
                Departamento = nombreDepartamento ?? requisicion.IdDepartamento?.ToString(),
                NomResponsableDepartamento = requisicion.NomResponsableDepartamento,
                CargoResponsableDepartamento = requisicion.IdDepartamentoNavigation.CargoJefe,
                NomDirector = requisicion.NombreDirector,
                CargoDirector = requisicion.IdDepartamentoNavigation.CargoDirector,
                Correo = requisicion.Correo,
                Telefono = requisicion.Telefono,
                LugarEntrega = requisicion.LugarEntrega,
                UsoEspecifico = requisicion.UsoEspecifico,
                Justificacion = requisicion.Justificacion,
                CuentaProgramaPresupuestario = requisicion.CuentaProgramaPresupuestario,
                UsoMaterial = requisicion.Donativo,
                Hash = requisicion.Hash,
                FechaServicio = requisicion.FechaServicio,
                TipoServicio = requisicion.TipoServicio,
                Articulos = articulos
            };
        }

        public async Task<bool> ActualizarRequisicion(int idRequisicion, FormularioRequisicionDTO modelo, int idUsuario)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.Correo = modelo.Correo;
            requisicion.Telefono = modelo.Telefono;
            requisicion.UsoEspecifico = modelo.UsoEspecifico;
            requisicion.LugarEntrega = modelo.LugarEntrega;
            requisicion.Justificacion = modelo.Justificacion;
            requisicion.IdEstatus = 1;
            requisicion.CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario;
            requisicion.Donativo = modelo.UsoMaterial;
            requisicion.FechaModificacion = DateTime.Now;
            requisicion.FechaServicio = modelo.FechaServicio;
            requisicion.TipoServicio = modelo.TipoServicio;

            await _repositoryRequisicion.Editar(requisicion);

            // Eliminar artículos anteriores
            var queryDetalles = await _repositoryRequisicionDetalle.Consultar(r => r.IdRequisicion == idRequisicion);
            var detallesActuales = await queryDetalles.ToListAsync();
            foreach (var detalle in detallesActuales)
                await _repositoryRequisicionDetalle.Eliminar(detalle);

            // Insertar los nuevos artículos 
            var nuevosDetalles = modelo.Articulos.Select(item => new TblRequisicionDetalle
            {
                IdRequisicion = idRequisicion,
                NumPartida = item.Cog,
                IdArticulo = item.IdArticulo,
                Cantidad = item.Cantidad,
                UnidadMedida = item.UnidadMedida,
                Descripcion = item.Descripcion,
                DescripcionDetallada = item.DescripcionDetallada.ToUpper(),
                FechaRegistro = DateTime.Now.Date
            }).ToList();

            await _repositoryRequisicionDetalle.CrearRango(nuevosDetalles);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = requisicion.IdEstatus,
                FechaEstatus = DateTime.Now,
                Observacion = "ModificaciónRequisiciones",
                IdUsuario = idUsuario
            };
            var bitacoraCreada = await _repositoryBitacora.Crear(bitacora);

            return true;
        }

        public async Task<bool> AsignarRequisicion(int idRequi, int idUsuario, int idUsuarioMat)
        {
            try
            {
                var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequi);

                if (requisicion == null)
                    return false;


                requisicion.IdUsuarioMat = idUsuarioMat;
                requisicion.IdEstatus = 2;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = requisicion.IdEstatus,
                    FechaEstatus = DateTime.Now,
                    Observacion = "AsignarRequisiciones",
                    IdUsuario = idUsuario
                };
                var bitacoraCreada = await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == modelo.IdRequisicion);
                if (requisicion == null) return false;

                // Actualizar COGs si vienen
                if (modelo.CogsEditados != null && modelo.CogsEditados.Any())
                {
                    var queryDetalles = await _repositoryRequisicionDetalle
                        .Consultar(d => d.IdRequisicion == modelo.IdRequisicion);
                    var detalles = await queryDetalles.ToListAsync();

                    foreach (var detalle in detalles)
                    {
                        var cogEditado = modelo.CogsEditados
                            .FirstOrDefault(c => c.IdArticulo == detalle.IdArticulo);
                        if (cogEditado != default && cogEditado.Cog != 0)
                            detalle.CogEditable = cogEditado.Cog;
                    }

                    foreach (var detalle in detalles)
                        await _repositoryRequisicionDetalle.Editar(detalle);
                }

                requisicion.IdEstatus = 13;
                requisicion.IdPp = modelo.IdPP;
                requisicion.Ff = modelo.FF;
                requisicion.TipoPrograma = modelo.TipoPrograma;
                requisicion.ClaveRegion = modelo.ClaveRegion;
                requisicion.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = 13,
                    FechaEstatus = DateTime.Now,
                    Observacion = modelo.Observaciones,
                    IdUsuario = idUsuario
                };
                await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch { throw; }
        }

        public async Task<string?> ObtenerObservacionesModificacion(int idRequisicion)
        {
            var query = await _repositoryBitacora.Consultar(b =>
                b.IdRequisicion == idRequisicion &&
                b.IdEstatus == 3);

            return await query
                .OrderByDescending(b => b.FechaEstatus)
                .Select(b => b.Observacion)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ProgresoPasoDTO>> ObtenerProgresoRequisicion(int idRequisicion)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null)
                return new List<ProgresoPasoDTO>();

            int idEstatusActual = requisicion.IdEstatus ?? 0;

            var todosEstatus = await _repositoryEstatus.Consultar();
            var nombresEstatus = await todosEstatus.ToDictionaryAsync(e => e.IdEstatus, e => e.NombreEstatus);

            var bitacorasQuery = await _repositoryBitacora.Consultar(b => b.IdRequisicion == idRequisicion);
            var eventos = await bitacorasQuery
                .OrderBy(b => b.FechaEstatus)
                .ThenBy(b => b.IdBitacoraEstatus)
                .Select(b => new
                {
                    b.IdBitacoraEstatus,
                    b.IdEstatus,
                    b.FechaEstatus,
                    b.Observacion,
                    Usuario = b.IdUsuarioNavigation != null ? b.IdUsuarioNavigation.Usuario : ""
                })
                .ToListAsync();

            var esMx = System.Globalization.CultureInfo.GetCultureInfo("es-MX");
            var resultado = new List<ProgresoPasoDTO>();

            for (int i = 0; i < eventos.Count; i++)
            {
                var ev = eventos[i];
                int idEst = ev.IdEstatus ?? 0;
                bool esUltimo = i == eventos.Count - 1;

                string state;
                if (!esUltimo)
                    state = "done";
                else if (EstatusFlow.EsTerminalNegativo(idEst))
                    state = "cancelled";
                else if (EstatusFlow.TerminalPositivos.Contains(idEst))
                    state = "completed";
                else
                    state = "active";

                string date = "—", time = "—";
                if (ev.FechaEstatus.HasValue)
                {
                    var dt = ev.FechaEstatus.Value;
                    date = dt.ToString("dd MMM yyyy", esMx);
                    time = dt.ToString("hh:mm tt", esMx);
                }

                resultado.Add(new ProgresoPasoDTO
                {
                    Dept = nombresEstatus.GetValueOrDefault(idEst, $"Estatus {idEst}"),
                    Date = date,
                    State = state,
                    By = ev.Usuario ?? "—",
                    Time = time,
                    Action = ev.Observacion ?? "",
                    Comment = ev.Observacion ?? ""
                });
            }

            // Paso sintético si la bitácora no refleja el estatus vigente
            if (idEstatusActual > 0 && (eventos.Count == 0 || eventos.Last().IdEstatus != idEstatusActual))
            {
                string synState = EstatusFlow.EsTerminalNegativo(idEstatusActual) ? "cancelled"
                    : EstatusFlow.TerminalPositivos.Contains(idEstatusActual) ? "completed"
                    : "active";

                string synDate = "—", synTime = "—";
                if (requisicion.FechaModificacion.HasValue)
                {
                    var dt = requisicion.FechaModificacion.Value;
                    synDate = dt.ToString("dd MMM yyyy", esMx);
                    synTime = dt.ToString("hh:mm tt", esMx);
                }

                resultado.Add(new ProgresoPasoDTO
                {
                    Dept = nombresEstatus.GetValueOrDefault(idEstatusActual, $"Estatus {idEstatusActual}"),
                    Date = synDate,
                    State = synState,
                    By = "—",
                    Time = synTime,
                    Action = "",
                    Comment = ""
                });
            }

            return resultado;
        }

        public async Task<bool> EnviarAAlmacen(int idRequisicion, int idUsuario)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 9;
            requisicion.FechaModificacion = DateTime.Now;
            await _repositoryRequisicion.Editar(requisicion);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = 9,
                FechaEstatus = DateTime.Now,
                Observacion = "Enviada a Almacén",
                IdUsuario = idUsuario
            };
            await _repositoryBitacora.Crear(bitacora);

            return true;
        }

        public async Task<bool> EnviarAModificacion(int idRequisicion, int idUsuario, string observaciones)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == idRequisicion);
                if (requisicion == null) return false;

                requisicion.IdEstatus = 3; // REQUISICION MODIFICACIÓN
                requisicion.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = 3,
                    FechaEstatus = DateTime.Now,
                    Observacion = observaciones,
                    IdUsuario = idUsuario
                };
                await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch { throw; }
        }

        public async Task<bool> RechazarRequisicion(int idRequisicion, int idUsuario, string motivo)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 5;
            requisicion.FechaModificacion = DateTime.Now;
            await _repositoryRequisicion.Editar(requisicion);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = 5,
                FechaEstatus = DateTime.Now,
                Observacion = motivo,
                IdUsuario = idUsuario
            };
            await _repositoryBitacora.Crear(bitacora);

            return true;
        }

        public async Task<bool> GuardarFotosRequisicion(int idRequisicion, List<IFormFile> fotos, string webRootPath)
        {
            var carpeta = Path.Combine(webRootPath, "uploads", "diseños", idRequisicion.ToString());
            Directory.CreateDirectory(carpeta);

            foreach (var foto in fotos)
            {
                if (foto.Length == 0) continue;

                var nombreArchivo = $"{Guid.NewGuid()}{Path.GetExtension(foto.FileName)}";
                var rutaCompleta = Path.Combine(carpeta, nombreArchivo);

                using (var stream = new FileStream(rutaCompleta, FileMode.Create))
                    await foto.CopyToAsync(stream);

                await _repositoryDisenos.Crear(new TblRegistroDiseno
                {
                    IdRequisicion = idRequisicion,
                    Ruta = $"/uploads/diseños/{idRequisicion}/{nombreArchivo}",
                    FechaSubida = DateTime.Now,
                    Tipo = "diseno"
                });
            }

            return true;
        }

        public async Task<List<string>> ObtenerFotosRequisicion(int idRequisicion)
        {
            var query = await _repositoryDisenos.Consultar(f => f.IdRequisicion == idRequisicion);
            return await query.Select(f => f.Ruta).ToListAsync();
        }

        public async Task<bool> EliminarFotoRequisicion(int idFoto)
        {
            var foto = await _repositoryDisenos.Obtener(f => f.Id == idFoto);
            if (foto == null) return false;

            await _repositoryDisenos.Eliminar(foto);
            return true;
        }

        public async Task<TblRegistroDiseno?> ObtenerFotoPorId(int idFoto)
        {
            return await _repositoryDisenos.Obtener(f => f.Id == idFoto);
        }

        public async Task<List<TblRegistroDiseno>> ObtenerFotosConIdRequisicion(int idRequisicion)
        {
            var query = await _repositoryDisenos.Consultar(f => f.IdRequisicion == idRequisicion &&
                f.Tipo == "diseno");
            return await query.ToListAsync();
        }

        public async Task<bool> GuardarArchivosAtencion(
            int idRequisicion,
            List<IFormFile> cotizaciones,
            List<IFormFile> cuadroComparativo,
            string webRootPath)
        {
            async Task Guardar(List<IFormFile> archivos, string carpetaNombre, string tipo)
            {
                if (!archivos.Any()) return;
                var carpeta = Path.Combine(webRootPath, "uploads", carpetaNombre, idRequisicion.ToString());
                Directory.CreateDirectory(carpeta);

                foreach (var archivo in archivos)
                {
                    if (archivo.Length == 0) continue;
                    var nombre = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
                    using (var stream = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create))
                        await archivo.CopyToAsync(stream);

                    await _repositoryDisenos.Crear(new TblRegistroDiseno
                    {
                        IdRequisicion = idRequisicion,
                        Ruta = $"/uploads/{carpetaNombre}/{idRequisicion}/{nombre}",
                        FechaSubida = DateTime.Now,
                        Tipo = tipo
                    });
                }
            }

            await Guardar(cotizaciones, "cotizaciones", "cotizacion");
            await Guardar(cuadroComparativo, "cuadro_comparativo", "cuadro_comparativo");
            return true;
        }

        private string GenerarSelloDigital()
        {
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
