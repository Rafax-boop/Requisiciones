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
    public class DAFService : IDAFService
    {
        private readonly IGenericRepository<TblRequisicion> _repositoryRequi;
        private readonly IGenericRepository<TblBitacoraEstatus> _repositoryBitacora;

        public DAFService(IGenericRepository<TblRequisicion> repositoryRequi, IGenericRepository<TblBitacoraEstatus> repositoryBitacora)
        {
            _repositoryRequi = repositoryRequi;
            _repositoryBitacora = repositoryBitacora;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisiciones()
        {
            IQueryable<TblRequisicion> query;
            query = await _repositoryRequi.Consultar();

            var resultado = await query
                .Where(r => r.IdEstatus == 15)
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

        public async Task<bool> RevisarRequisicion(int idRequisicion, int idUsuario)
        {
            try
            {
                var requisicion = await _repositoryRequi
                    .Obtener(r => r.IdRequisicion == idRequisicion);

                if (requisicion == null) return false;

                requisicion.IdEstatus = 16;
                requisicion.FechaModificacion = DateTime.Now;

                await _repositoryRequi.Editar(requisicion);

                var bitacora = new TblBitacoraEstatus
                {
                    IdRequisicion = idRequisicion,
                    IdEstatus = 16,
                    FechaEstatus = DateTime.Now,
                    Observacion = "Revisado por el area de DAF",
                    IdUsuario = idUsuario
                };

                await _repositoryBitacora.Crear(bitacora);

                return true;
            }
            catch { throw; }
        }
    }
}
