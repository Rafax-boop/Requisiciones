using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class FinancierosService : IFinancierosService
    {
        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblBitacoraEstatus> _repositoryBitacora;

        public FinancierosService(IRequisicionRepository repositoryRequisicion, IGenericRepository<TblBitacoraEstatus> repositoryBitacora)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryBitacora = repositoryBitacora;
        }
        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idUsuarioFinancieros = null)
        {
            IQueryable<TblRequisicion> query;

            if (idUsuarioFinancieros.HasValue)
            {
                query = await _repositoryRequisicion.Consultar(r => r.IdUsuarioFinan == idUsuarioFinancieros.Value);
            }
            else
            {
                query = await _repositoryRequisicion.Consultar();
            }

            var resultado = await query
                .Where(r => r.IdUsuarioFinan.HasValue || r.IdEstatus == 13)
                .OrderBy(r => r.FechaModificacion)
                .ThenBy(r => r.IdRequisicion)
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
                        .Where(b => b.IdEstatus == 14)
                        .OrderByDescending(b => b.FechaEstatus)
                        .Select(b => (DateTime.Now - (b.FechaEstatus ?? DateTime.Now)).Days)
                        .FirstOrDefault(),
                    NombreAsignado = r.IdUsuarioFinanNavigation != null ? r.IdUsuarioFinanNavigation.Usuario : null,
                    RequiServicio = r.RequiServicio
                })
                .ToListAsync();

            return resultado;
        }

        public async Task<bool> AsignarRequisicion(int idRequi, int idUsuario, int idUsuarioFinan)
        {
            try
            {
                var requisicion = await _repositoryRequisicion.Obtener(r => r.IdRequisicion == idRequi);

                if (requisicion == null)
                    return false;


                requisicion.IdUsuarioFinan = idUsuarioFinan;
                requisicion.IdEstatus = 14;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = requisicion.IdEstatus,
                    FechaEstatus = DateTime.Now,
                    Observacion = "AsignarFinancieros",
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

                requisicion.IdEstatus = 4;
                requisicion.FechaModificacion = DateTime.Now;
                await _repositoryRequisicion.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = requisicion.IdRequisicion,
                    IdEstatus = 4,
                    FechaEstatus = DateTime.Now,
                    Observacion = modelo.Observaciones,
                    IdUsuario = idUsuario
                };
                await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch { throw; }
        }
    }
}
