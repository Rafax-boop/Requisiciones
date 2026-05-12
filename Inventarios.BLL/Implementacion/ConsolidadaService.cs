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
    public class ConsolidadaService : IConsolidadaService
    {
        private readonly IGenericRepository<TblRequisicion> _repoRequisicion;
        private readonly IGenericRepository<TblConsolidada> _repoConsolidada;
        private readonly IGenericRepository<TblConsolidadasDetalle> _repoDetalle;

        public ConsolidadaService(
            IGenericRepository<TblRequisicion> repoRequisicion,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repoDetalle)
        {
            _repoRequisicion = repoRequisicion;
            _repoConsolidada = repoConsolidada;
            _repoDetalle = repoDetalle;
        }

        public async Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConsolidables(int idUsuario)
        {
            var query = await _repoRequisicion.Consultar(r =>
                r.IdUsuarioMat == idUsuario &&
                r.IdEstatus == 2 &&
                r.ConsolidadaId == null);   // ← campo que agregaste con el ALTER

            return await query.Select(r => new RequisicionMaestraDTO
            {
                IdRequi = r.IdRequisicion,
                NumRequi = r.NumRequisicion,
                FechaEmision = r.FechaEmision,
                Departamento = r.IdDepartamentoNavigation.NombreDepartamento,
                Responsable = r.NomResponsableDepartamento,
                CantidadPartidas = r.TblRequisicionDetalles.Count
            }).ToListAsync();
        }

        public async Task<TblConsolidada> CrearConsolidada(List<int> idsRequisiciones, int idUsuario)
        {
            // Verificar que ninguna esté ya consolidada
            var query = await _repoRequisicion.Consultar(r =>
                idsRequisiciones.Contains(r.IdRequisicion));
            var requis = await query.ToListAsync();

            if (requis.Any(r => r.ConsolidadaId != null))
                return null;

            // Generar folio: CONS-YYYY-NNN
            var totalQuery = await _repoConsolidada.Consultar();
            var total = await totalQuery.CountAsync();
            var folio = $"CONS-{DateTime.Now.Year}-{(total + 1):D3}";
            var hash = BitConverter.ToString(
                System.Security.Cryptography.RandomNumberGenerator.GetBytes(8)
            ).Replace("-", "");

            // Crear encabezado
            var consolidada = new TblConsolidada
            {
                FolioConsolidada = folio,
                Hash = hash,
                IdUsuario = idUsuario,
                IdEstatus = 1,
                FechaCreacion = DateTime.Now,
                FechaModificacion = DateTime.Now
            };
            var creada = await _repoConsolidada.Crear(consolidada);

            // Crear detalles (tabla puente)
            var detalles = idsRequisiciones.Select(id => new TblConsolidadasDetalle
            {
                ConsolidadaId = creada.ConsolidadaId,
                IdRequisicion = id,
                FechaAgregada = DateTime.Now,
                AgregadoPor = idUsuario
            }).ToList();
            await _repoDetalle.CrearRango(detalles);

            // Marcar cada requi como absorbida
            foreach (var r in requis)
            {
                r.ConsolidadaId = creada.ConsolidadaId;
                await _repoRequisicion.Editar(r);
            }

            return creada;
        }
    }
}
