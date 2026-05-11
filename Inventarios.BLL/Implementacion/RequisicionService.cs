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
        private readonly IGenericRepository<TblRequisicionDetalleMovimiento> _repoMovimiento;
        private readonly IGenericRepository<TblRequisicionDetalleMunicipio> _repoMunicipiosDetalle;
        private readonly IUnitOfWork _unitOfWork;

        public RequisicionService(
            IRequisicionRepository repositoryRequisicion,
            IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle,
            IGenericRepository<TblBitacoraEstatus> repositoryBitacora,
            IGenericRepository<TblDepartamento> repositoryDepartamento,
            IGenericRepository<TblEstatus> repositoryEstatus,
            IGenericRepository<TblRegistroDiseno> repositoryDisenos,
            IGenericRepository<TblArticulosProgramado> repositoryProgramacion,
            IUnitOfWork unitOfWork,
            IGenericRepository<TblRequisicionDetalleMovimiento> repoMovimiento,
            IGenericRepository<TblRequisicionDetalleMunicipio> repoMunicipiosDetalle
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
            _repoMovimiento = repoMovimiento;
            _repoMunicipiosDetalle = repoMunicipiosDetalle;
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
                    FechaRegistro = DateTime.Now.Date,
                    // IdMunicipio = item.IdMunicipio > 0 ? item.IdMunicipio : null
                };
                listaArticulos.Add(detalle);
            }

            await _repositoryRequisicionDetalle.CrearRango(listaArticulos);

            // Guardar programaciÃ³n si algÃºn artÃ­culo la trae
            var listaProgramacion = new List<TblArticulosProgramado>();

            for (int i = 0; i < modelo.Articulos.Count; i++)
            {
                var item = modelo.Articulos[i];

                if (string.IsNullOrEmpty(item.TipoProgramacion)) continue;

                var detalle = listaArticulos[i]; // â† mismo Ã­ndice, siempre correcto

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
                    Mes = item.TipoProgramacion == "mensual" ? item.Mes : null,
                };
                listaProgramacion.Add(prog);
            }

            if (listaProgramacion.Any())
                await _repositoryProgramacion.CrearRango(listaProgramacion);

            // Guardar distribuciÃ³n por municipio
            var listaMunicipios = new List<TblRequisicionDetalleMunicipio>();

            for (int i = 0; i < modelo.Articulos.Count; i++)
            {
                var item = modelo.Articulos[i];
                if (item.Municipios == null || !item.Municipios.Any()) continue;

                var detalle = listaArticulos[i];

                foreach (var muni in item.Municipios)
                {
                    if (muni.IdMunicipio <= 0) continue;
                    listaMunicipios.Add(new TblRequisicionDetalleMunicipio
                    {
                        IdRequisicion = requiCreada.IdRequisicion,
                        IdRequisicionDetalle = detalle.IdRequisicionDetalle,
                        IdMunicipio = muni.IdMunicipio,
                        Cantidad = muni.Cantidad,
                        FechaRegistro = DateTime.Now
                    });
                }
            }

            if (listaMunicipios.Any())
                await _repoMunicipiosDetalle.CrearRango(listaMunicipios);

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
                    NombreAsignado = r.IdUsuarioMatNavigation != null ? r.IdUsuarioMatNavigation.Usuario : null,
                    ComentarioRechazo = r.TblBitacoraEstatuses
                        .Where(b => b.IdEstatus == 5)
                        .OrderByDescending(b => b.FechaEstatus)
                        .Select(b => b.Observacion)
                        .FirstOrDefault()
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

            var queryMovimientos = await _repoMovimiento.Consultar(
                m => m.IdRequisicion == idMaestro);
            var movimientos = await queryMovimientos.ToListAsync();

            // Para cada partida, el movimiento mÃ¡s relevante (Ãºltimo por fecha)
            var movimientoPorPartida = movimientos
                .GroupBy(m => m.IdRequisicionDetalle)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(m => m.FechaMovimiento).First()
                );

            // Traer TODAS las partidas sin filtrar
            var query = await _repositoryRequisicionDetalle
                .Consultar(r => r.IdRequisicion == idMaestro);

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

            // Calcular EstatusPartida por cada artÃ­culo
            bool requiEntregada = maestra?.IdEstatus == 12;
            foreach (var art in lista)
            {
                if (movimientoPorPartida.TryGetValue(art.IdRequisicionDetalle, out var mov))
                {
                    art.EstatusPartida = mov.TipoMovimiento switch
                    {
                        "COMPRA" => (mov.Confirmado.GetValueOrDefault()) ? "Entregado" : "En compra",
                        "ENTREGA" => (mov.Confirmado.GetValueOrDefault()) ? "Entregado" : "En entrega",
                        _ => "En compra"
                    };
                }
                else
                {
                    art.EstatusPartida = requiEntregada ? "Entregado"
                        : maestra?.IdEstatus == 7 ? "En entrega"
                        : "En compra";
                }
            }

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

            var observacion = await (await _repositoryBitacora.Consultar(b =>
                b.IdRequisicion == idMaestro && b.IdEstatus == maestra.IdEstatus))
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

            var queryAnexos = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "anexo");
            var anexos = await queryAnexos
                .Select(f => new ArchivoAtencionDTO
                {
                    Ruta = f.Ruta,
                    NombreArchivo = Path.GetFileName(f.Ruta)
                }).ToListAsync();

            var querySIAF = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "SIAF");
            var archivosSiaf = await querySIAF  // â† querySIAF, no queryCuadro
                .Select(f => new ArchivoAtencionDTO
                {
                    Ruta = f.Ruta,
                    NombreArchivo = Path.GetFileName(f.Ruta)
                }).ToListAsync();

            var queryTablaApi = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "TablaApi");
            var archivosTablaApi = await queryTablaApi
                .Select(f => new ArchivoAtencionDTO
                {
                    Ruta = f.Ruta,
                    NombreArchivo = Path.GetFileName(f.Ruta)
                }).ToListAsync();
            var queryPedido = await _repositoryDisenos.Consultar(
                f => f.IdRequisicion == idMaestro && f.Tipo == "pedido_compra");
            var archivosPedidos = await queryPedido
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
                Articulos = lista,
                Cotizaciones = cotizaciones,
                CuadroComparativo = cuadro,
                Anexos = anexos,
                Observaciones = observacion,
                ArchivosSiaf = archivosSiaf,
                ArchivosTablaApi = archivosTablaApi,
                ArchivosPedidoCompra = archivosPedidos,
                IdEstatus = maestra?.IdEstatus ?? 0,
                NumeroApi = maestra?.NumApi
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
                    ClaveMaterial = r.IdArticuloNavigation.Clave,
                    IdArticulo = r.IdArticulo,
                    Cantidad = r.Cantidad,
                    UnidadMedida = r.UnidadMedida,
                    Descripcion = r.Descripcion,
                    DescripcionDetallada = r.DescripcionDetallada
                })
                .ToListAsync();

            var queryProgramacion = await _repositoryProgramacion.Consultar(p => p.IdRequisicion == idRequisicion);
            var filasProgramacion = await queryProgramacion.ToListAsync();
            foreach (var art in articulos)
            {
                var prog = filasProgramacion.FirstOrDefault(p => p.IdRequisicionDetalle == art.IdRequisicionDetalle);
                if (prog == null) continue;
                art.TipoProgramacion = prog.TipoProgramacion;
                art.Llenado1 = prog.Llenado1;
                art.Llenado2 = prog.Llenado2;
                art.Llenado3 = prog.Llenado3;
                art.Llenado4 = prog.Llenado4;
                art.Llenado5 = prog.Llenado5;
                art.Llenado6 = prog.Llenado6;
                art.Llenado7 = prog.Llenado7;
                art.Llenado8 = prog.Llenado8;
                art.Llenado9 = prog.Llenado9;
                art.Llenado10 = prog.Llenado10;
                art.Llenado11 = prog.Llenado11;
                art.Llenado12 = prog.Llenado12;
                art.Mes = prog.Mes;
            }

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
                IdAdquisicion = requisicion.IdAdjudicacion,
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

            // Eliminar artÃ­culos anteriores
            var queryDetalles = await _repositoryRequisicionDetalle.Consultar(r => r.IdRequisicion == idRequisicion);
            var detallesActuales = await queryDetalles.ToListAsync();
            foreach (var detalle in detallesActuales)
                await _repositoryRequisicionDetalle.Eliminar(detalle);

            // Insertar los nuevos artÃ­culos 
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

            // Eliminar municipios anteriores y reinsertar con los nuevos IdRequisicionDetalle
            var queryMunis = await _repoMunicipiosDetalle.Consultar(m => m.IdRequisicion == idRequisicion);
            var municipiosActuales = await queryMunis.ToListAsync();
            foreach (var muni in municipiosActuales)
                await _repoMunicipiosDetalle.Eliminar(muni);

            var listaMunicipios = new List<TblRequisicionDetalleMunicipio>();
            for (int i = 0; i < modelo.Articulos.Count; i++)
            {
                var item = modelo.Articulos[i];
                if (item.Municipios == null || !item.Municipios.Any()) continue;

                var detalle = nuevosDetalles[i];
                foreach (var muniItem in item.Municipios)
                {
                    if (muniItem.IdMunicipio <= 0) continue;
                    listaMunicipios.Add(new TblRequisicionDetalleMunicipio
                    {
                        IdRequisicion = idRequisicion,
                        IdRequisicionDetalle = detalle.IdRequisicionDetalle,
                        IdMunicipio = muniItem.IdMunicipio,
                        Cantidad = muniItem.Cantidad,
                        FechaRegistro = DateTime.Now
                    });
                }
            }
            if (listaMunicipios.Any())
                await _repoMunicipiosDetalle.CrearRango(listaMunicipios);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = requisicion.IdEstatus,
                FechaEstatus = DateTime.Now,
                Observacion = "ModificaciÃ³nRequisiciones",
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

        public async Task<bool> AceptarExpediente(int idRequisicion, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == idRequisicion);

                if (requisicion == null) return false;

                requisicion.IdEstatus = 17;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = idRequisicion,
                    IdEstatus = 17,
                    FechaEstatus = DateTime.Now,
                    Observacion = "Enviado a proceso de pago",
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

                string date = "â€”", time = "â€”";
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
                    By = ev.Usuario ?? "â€”",
                    Time = time,
                    Action = ev.Observacion ?? "",
                    Comment = ev.Observacion ?? ""
                });
            }

            // Paso sintÃ©tico si la bitÃ¡cora no refleja el estatus vigente
            if (idEstatusActual > 0 && (eventos.Count == 0 || eventos.Last().IdEstatus != idEstatusActual))
            {
                string synState = EstatusFlow.EsTerminalNegativo(idEstatusActual) ? "cancelled"
                    : EstatusFlow.TerminalPositivos.Contains(idEstatusActual) ? "completed"
                    : "active";

                string synDate = "â€”", synTime = "â€”";
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
                    By = "â€”",
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
                Observacion = "Enviada a AlmacÃ©n",
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

                requisicion.IdEstatus = 3; // REQUISICION MODIFICACIÃ“N
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
            var carpeta = Path.Combine(webRootPath, "uploads", "diseÃ±os", idRequisicion.ToString());
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
                    Ruta = $"/uploads/diseÃ±os/{idRequisicion}/{nombreArchivo}",
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
            //List<IFormFile> cotizaciones,
            List<IFormFile> cuadroComparativo,
            List<IFormFile> anexos,
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

           // await Guardar(cotizaciones, "cotizaciones", "cotizacion");
            await Guardar(cuadroComparativo, "cuadro_comparativo", "cuadro_comparativo");
            await Guardar(anexos, "Anexos", "anexo");
            return true;
        }

        public async Task<bool> SubirDocumentoProveedor(int idRequisicion, string tipoDocumento,
    IFormFile archivo, string webRootPath, int idUsuario)
        {
            if (archivo == null || archivo.Length == 0) return false;

            var carpeta = Path.Combine(webRootPath, "uploads", "Proveedor", idRequisicion.ToString());
            Directory.CreateDirectory(carpeta);

            var nombre = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            using (var stream = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create))
                await archivo.CopyToAsync(stream);

            // Eliminar versiÃ³n anterior del mismo tipo si existe
            var queryPrev = await _repositoryDisenos.Consultar(f =>
                f.IdRequisicion == idRequisicion && f.Tipo == $"proveedor_{tipoDocumento}");
            var previos = await queryPrev.ToListAsync();
            foreach (var p in previos)
                await _repositoryDisenos.Eliminar(p);

            await _repositoryDisenos.Crear(new TblRegistroDiseno
            {
                IdRequisicion = idRequisicion,
                Ruta = $"/uploads/Proveedor/{idRequisicion}/{nombre}",
                FechaSubida = DateTime.Now,
                Tipo = $"proveedor_{tipoDocumento}"
            });

            return true;
        }

        public async Task<List<ArchivoAtencionDTO>> ObtenerDocumentosProveedor(int idRequisicion)
        {
            var query = await _repositoryDisenos.Consultar(f =>
                f.IdRequisicion == idRequisicion && f.Tipo.StartsWith("proveedor_"));

            return await query.Select(f => new ArchivoAtencionDTO
            {
                Ruta = f.Ruta,
                NombreArchivo = f.Tipo.Replace("proveedor_", "")
            }).ToListAsync();
        }

        public async Task<bool> EnviarAFinancierosConDocs(int idRequisicion, int idUsuario, IFormFile archivoPedido, string webRootPath)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            if (archivoPedido != null)
            {
                await SubirDocumentoPedido(idRequisicion, archivoPedido, webRootPath);
            }

            requisicion.IdEstatus = 17;
            requisicion.FechaModificacion = DateTime.Now;
            await _repositoryRequisicion.Editar(requisicion);

            await _repositoryBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = idRequisicion,
                IdEstatus = 17,
                FechaEstatus = DateTime.Now,
                Observacion = "Documentos del proveedor enviados a revisión",
                IdUsuario = idUsuario
            });

            return true;
        }

        public async Task<bool> RebotarDocumentos(int idRequisicion, string observaciones,
            List<string> docsObservados, int idUsuario)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 18;
            requisicion.FechaModificacion = DateTime.Now;
            await _repositoryRequisicion.Editar(requisicion);

            var notaCompleta = $"DOCUMENTOS OBSERVADOS: {string.Join(", ", docsObservados)}. " +
                               $"NOTA: {observaciones}";

            await _repositoryBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = idRequisicion,
                IdEstatus = 18,
                FechaEstatus = DateTime.Now,
                Observacion = notaCompleta,
                IdUsuario = idUsuario
            });

            return true;
        }

        public async Task<bool> FinalizarRequisicion(int idRequisicion, string observaciones, int idUsuario)
        {
            var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 12;
            requisicion.FechaModificacion = DateTime.Now;
            await _repositoryRequisicion.Editar(requisicion);

            await _repositoryBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = idRequisicion,
                IdEstatus = 12,
                FechaEstatus = DateTime.Now,
                Observacion = observaciones,
                IdUsuario = idUsuario
            });

            return true;
        }

        private string GenerarSelloDigital()
        {
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    
        public async Task<bool> SubirDocumentoPedido(int idRequisicion, IFormFile archivo, string webRootPath)
        {
            if (archivo == null || archivo.Length == 0) return false;

            var carpeta = Path.Combine(webRootPath, "uploads", "PedidoCompra", idRequisicion.ToString());
            Directory.CreateDirectory(carpeta);

            var nombre = $"{Guid.NewGuid()}{Path.GetExtension(archivo.FileName)}";
            using (var stream = new FileStream(Path.Combine(carpeta, nombre), FileMode.Create))
                await archivo.CopyToAsync(stream);

            await _repositoryDisenos.Crear(new TblRegistroDiseno
            {
                IdRequisicion = idRequisicion,
                Ruta = $"/uploads/PedidoCompra/{idRequisicion}/{nombre}",
                FechaSubida = DateTime.Now,
                Tipo = "pedido_compra"
            });

            return true;
        }

        public async Task<Dictionary<int, List<MunicipioItemDTO>>> ObtenerDistribucionMunicipiosPorRequisicion(int idRequisicion)
        {
            var query = await _repoMunicipiosDetalle.Consultar(
                m => m.IdRequisicion == idRequisicion);

            var municipios = await query
                .Include(m => m.IdMunicipioNavigation)
                .ToListAsync();

            var distribucion = municipios
                .GroupBy(m => m.IdRequisicionDetalle)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(m => new MunicipioItemDTO
                    {
                        IdMunicipio = m.IdMunicipio,
                        Cantidad = m.Cantidad
                    }).ToList()
                );

            return distribucion;
        }

        public async Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConArchivos(int? idDepartamento, bool servicio, int? idUsuarioMat = null)
        {
            var query = await _repositoryRequisicion.Consultar(r => r.RequiServicio == servicio);

            if (idDepartamento.HasValue)
                query = query.Where(r => r.IdDepartamento == idDepartamento.Value);

            if (idUsuarioMat.HasValue)
                query = query.Where(r => r.IdUsuarioMat == idUsuarioMat.Value);

            var archivoIds = await _repositoryDisenos.Consultar(a => true);
            var idsConArchivos = await archivoIds.Select(a => a.IdRequisicion).Distinct().ToListAsync();

            var resultado = await query
                .Where(r => idsConArchivos.Contains(r.IdRequisicion))
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

        public async Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConArchivosTodos(int? idDepartamento)
        {
            var query = await _repositoryRequisicion.Consultar(r => true);

            if (idDepartamento.HasValue)
                query = query.Where(r => r.IdDepartamento == idDepartamento.Value);

            var archivoIds = await _repositoryDisenos.Consultar(a => true);
            var idsConArchivos = await archivoIds.Select(a => a.IdRequisicion).Distinct().ToListAsync();

            var resultado = await query
                .Where(r => idsConArchivos.Contains(r.IdRequisicion))
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

        public async Task<List<TblRegistroDiseno>> ObtenerTodosLosArchivosDeRequisicion(int idRequisicion)
        {
            var query = await _repositoryDisenos.Consultar(a => a.IdRequisicion == idRequisicion);
            return await query.OrderByDescending(a => a.FechaSubida).ToListAsync();
        }

        public async Task<DescargaArchivosRequisicionDTO?> ObtenerDatosDescargaArchivos(int idRequisicion)
        {
            var query = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);

            return await query
                .Select(r => new DescargaArchivosRequisicionDTO
                {
                    NumRequisicion = r.NumRequisicion,
                    NumApi = r.NumApi,
                    NumPedido = r.NumPedido
                })
                .FirstOrDefaultAsync();
        }
    }
}

