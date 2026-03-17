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
    public class AlmacenService : IAlmacenService
    {
        private readonly IRequisicionRepository _repositoryRequisicion;
        private readonly IGenericRepository<TblInventario> _repoInventario;
        private readonly IGenericRepository<TblEstatus> _repoEstatus;

        public AlmacenService(IRequisicionRepository requisicionRepository, IGenericRepository<TblInventario> repoInventario, IGenericRepository<TblEstatus> repoEstatus)
        {
            _repositoryRequisicion = requisicionRepository;
            _repoEstatus = repoEstatus;
            _repoInventario = repoInventario;
        }

        public async Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen()
        {
            IQueryable<TblRequisicion> query = await _repositoryRequisicion.Consultar(r => r.IdEstatus == 9);

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

        public async Task<List<string>> ObtenerEstatus()
        {
            var query = await _repoEstatus.Consultar();
            var entities = await query.ToListAsync();
            return entities
                .Select(e => e.NombreEstatus)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList();
        }

        public async Task<List<InventarioItemDTO>> ObtenerInventario()
        {
            var query = await _repoInventario.Consultar();
            var entities = await query.ToListAsync();

            return entities.Select(i => new InventarioItemDTO
            {
                Descripcion = i.Descripcion ?? "",
                UnidadMedida = i.UnidadMedida ?? "",
                Existencia = i.Existencia,
                Minimo = 0,
                Situacion = i.Existencia == 0 ? "Sin stock" : "OK"
            }).ToList();
        }
    }
}
