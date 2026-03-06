using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
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

        public RequisicionService(IRequisicionRepository repositoryRequisicion, IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle, IGenericRepository<TblBitacoraEstatus> repositoryBitacora, IGenericRepository<TblDepartamento> repositoryDepartamento, IGenericRepository<TblEstatus> repositoryEstatus)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryRequisicionDetalle = repositoryRequisicionDetalle;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDepartamento = repositoryDepartamento;
            _repositoryEstatus = repositoryEstatus;
        }

        public async Task<TblRequisicion> CrearRequisicion(FormularioRequisicionDTO modelo, int idUsuario)
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
                UsoEspecifico = modelo.UsoEspecifico.ToUpper(),
                Justificacion = modelo.Justificacion.ToUpper(),
                CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario,
                Donativo = modelo.UsoMaterial,
                IdUsuario = idUsuario,
                IdEstatus = 1,
                Activo = true,
                FechaModificacion = DateTime.Now,
                Hash = GenerarSelloDigital()
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

            return requiCreada;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idDepartamento, int? idUsuarioMat = null)
        {
            IQueryable<TblRequisicion> query;

            if (idUsuarioMat.HasValue)
            {
                // Rol 3: solo ve las que tiene asignadas
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

            var resultado = await query
                .Select(r => new RequisicionMaestraDTO
                {
                    IdRequi = r.IdRequisicion,
                    NumRequi = r.NumRequisicion,
                    FechaEmision = r.FechaEmision,
                    FechaModificacion = r.FechaModificacion,
                    Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                    Responsable = r.NomResponsableDepartamento,
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

        public async Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro)
        {
            var queryMaestra = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idMaestro);
            var maestra = await queryMaestra.FirstOrDefaultAsync();

            var query = await _repositoryRequisicionDetalle.Consultar(r => r.IdRequisicion == idMaestro);
            var lista = await query
                .Select(r => new DetalleArticuloDTO
                {
                    NumPartida = r.NumPartida,
                    IdArticulo = r.IdArticulo,
                    Cantidad = r.Cantidad,
                    UnidadMedida = r.UnidadMedida,
                    Descripcion = r.Descripcion,
                    DescripcionDetallada = r.DescripcionDetallada
                })
                .ToListAsync();

            return new DetallesRequiDTO
            {
                Donativo = maestra?.Donativo ?? false,
                IdPp = maestra?.IdPp,
                Ff = maestra?.Ff,
                TipoPrograma = maestra?.TipoPrograma,
                ClaveRegion = maestra?.ClaveRegion,
                Articulos = lista
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
                    NumPartida = r.NumPartida,
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
                NomDirector = requisicion.NombreDirector,
                Correo = requisicion.Correo,
                Telefono = requisicion.Telefono,
                LugarEntrega = requisicion.LugarEntrega,
                UsoEspecifico = requisicion.UsoEspecifico,
                Justificacion = requisicion.Justificacion,
                CuentaProgramaPresupuestario = requisicion.CuentaProgramaPresupuestario,
                UsoMaterial = requisicion.Donativo,
                Hash = requisicion.Hash,
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
            requisicion.Justificacion = modelo.Justificacion;
            requisicion.IdEstatus = 1;
            requisicion.CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario;
            requisicion.Donativo = modelo.UsoMaterial;
            requisicion.FechaModificacion = DateTime.Now;

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

        public async Task<bool> AtenderRequisicion(
            int idRequisicion, string observaciones,
            bool requiereModificacion, int idUsuario,
            int idpp, string FF, string tipoPrograma,
            int claveRegion,
            List<(int IdArticulo, int Cog)> cogsEditados = null
        )
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == idRequisicion);
                if (requisicion == null) return false;

                // Actualizar COGs si vienen
                if (cogsEditados != null && cogsEditados.Any())
                {
                    var queryDetalles = await _repositoryRequisicionDetalle
                        .Consultar(d => d.IdRequisicion == idRequisicion);
                    var detalles = await queryDetalles.ToListAsync();

                    foreach (var detalle in detalles)
                    {
                        var cogEditado = cogsEditados
                            .FirstOrDefault(c => c.IdArticulo == detalle.IdArticulo);
                        if (cogEditado != default && cogEditado.Cog != 0)
                            detalle.CogEditable = cogEditado.Cog;
                    }

                    foreach (var detalle in detalles)
                        await _repositoryRequisicionDetalle.Editar(detalle);
                }

                int nuevoEstatus = requiereModificacion ? 3 : 4;
                requisicion.IdEstatus = nuevoEstatus;
                requisicion.IdPp = idpp;
                requisicion.Ff = FF;
                requisicion.TipoPrograma = tipoPrograma;
                requisicion.ClaveRegion = claveRegion;
                requisicion.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = nuevoEstatus,
                    FechaEstatus = DateTime.Now,
                    Observacion = observaciones,
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
            var todosEstatus = await _repositoryEstatus.Consultar(e => e.Actvio);
            var listaEstatus = await todosEstatus.OrderBy(e => e.IdEstatus).ToListAsync();

            var bitacoras = await _repositoryBitacora.Consultar(b => b.IdRequisicion == idRequisicion);
            var bitacorasConUsuario = await bitacoras
                .OrderByDescending(b => b.FechaEstatus)
                .Select(b => new { b.IdEstatus, b.FechaEstatus, b.Observacion, Usuario = b.IdUsuarioNavigation != null ? b.IdUsuarioNavigation.Usuario : "" })
                .ToListAsync();

            var resultado = new List<ProgresoPasoDTO>();
            foreach (var est in listaEstatus)
            {
                string state = "pending";
                if (est.IdEstatus < idEstatusActual) state = "done";
                else if (est.IdEstatus == idEstatusActual) state = "active";

                var ultimaBitacora = bitacorasConUsuario.FirstOrDefault(b => b.IdEstatus == est.IdEstatus);
                string date = "—";
                string time = "—";
                string by = "—";
                string comment = "";
                if (ultimaBitacora != null && ultimaBitacora.FechaEstatus.HasValue)
                {
                    var dt = ultimaBitacora.FechaEstatus.Value;
                    date = dt.ToString("dd MMM yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
                    time = state == "active" && dt.Date == DateTime.Now.Date ? "En curso" : dt.ToString("hh:mm tt", System.Globalization.CultureInfo.GetCultureInfo("es-MX"));
                    by = ultimaBitacora.Usuario ?? "—";
                    comment = ultimaBitacora.Observacion ?? "";
                }

                resultado.Add(new ProgresoPasoDTO
                {
                    Dept = est.NombreEstatus,
                    Date = date,
                    State = state,
                    By = by,
                    Time = time,
                    Action = comment,
                    Comment = comment
                });
            }
            return resultado;
        }

        private string GenerarSelloDigital()
        {
            var bytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(8);
            return BitConverter.ToString(bytes).Replace("-", "");
        }
    }
}
