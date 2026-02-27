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

        public RequisicionService(IRequisicionRepository repositoryRequisicion, IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle, IGenericRepository<TblBitacoraEstatus> repositoryBitacora, IGenericRepository<TblDepartamento> repositoryDepartamento)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryRequisicionDetalle = repositoryRequisicionDetalle;
            _repositoryBitacora = repositoryBitacora;
            _repositoryDepartamento = repositoryDepartamento;
        }

        public async Task<bool> CrearRequisicion(FormularioRequisicionDTO modelo, int idUsuario)
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
                UsoEspecifico = modelo.UsoEspecifico,
                Justificacion = modelo.Justificacion,
                CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario,
                Donativo = modelo.UsoMaterial,
                IdUsuario = idUsuario,
                IdEstatus = 1,
                Activo = true,
                FechaSistema = DateTime.Now
            };
            var requiCreada = await _repositoryRequisicion.CrearConFolio(requisicion);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requiCreada.IdRequisicion,
                IdEstatus = requiCreada.IdEstatus,
                FechaEstatus = requiCreada.FechaSistema,
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
                    DescripcionDetallada = item.DescripcionDetallada,
                    FechaRegistro = DateTime.Now.Date
                };
                listaArticulos.Add(detalle);
            }

            await _repositoryRequisicionDetalle.CrearRango(listaArticulos);

            return true;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int idDepartamento, int? idUsuarioMat = null)
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
                DescripcionDetallada = item.DescripcionDetallada,
                FechaRegistro = DateTime.Now.Date
            }).ToList();

            await _repositoryRequisicionDetalle.CrearRango(nuevosDetalles);

            var bitacora = new TblBitacoraEstatus
            {
                IdRequisicion = requisicion.IdRequisicion,
                IdEstatus = requisicion.IdEstatus,
                FechaEstatus = requisicion.FechaSistema,
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

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = requisicion.IdEstatus,
                    FechaEstatus = requisicion.FechaSistema,
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

        public async Task<bool> AtenderRequisicion(int idRequisicion, string observaciones, bool requiereModificacion, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequisicion
                    .Obtener(r => r.IdRequisicion == idRequisicion);

                if (requisicion == null)
                    return false;


                int nuevoEstatus = requiereModificacion
                    ? 3
                    : 4;

                requisicion.IdEstatus = nuevoEstatus;
                requisicion.FechaSistema = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                // 🔹 Crear bitácora
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
            catch
            {
                throw;
            }
        }
    }
}
