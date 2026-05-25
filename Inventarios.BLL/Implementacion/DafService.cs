using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Microsoft.EntityFrameworkCore;

namespace Inventario.BLL.Implementacion
{
    public class DafService : IDafService
    {
        private readonly IGenericRepository<TblRequisicion> _repoRequisicion;
        private readonly IGenericRepository<TblConsolidada> _repoConsolidada;
        private readonly IGenericRepository<TblConsolidadasDetalle> _repoConsolidadaDetalle;
        private readonly IGenericRepository<TblBitacoraEstatus> _repoBitacora;

        public DafService(
            IGenericRepository<TblRequisicion> repoRequisicion,
            IGenericRepository<TblConsolidada> repoConsolidada,
            IGenericRepository<TblConsolidadasDetalle> repoConsolidadaDetalle,
            IGenericRepository<TblBitacoraEstatus> repoBitacora)
        {
            _repoRequisicion = repoRequisicion;
            _repoConsolidada = repoConsolidada;
            _repoConsolidadaDetalle = repoConsolidadaDetalle;
            _repoBitacora = repoBitacora;
        }

        public async Task<bool> AutorizarRequisicion(int idRequisicion, int idUsuario, string? observaciones)
        {
            var requisicion = await _repoRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 17;
            requisicion.FechaModificacion = DateTime.Now;
            await _repoRequisicion.Editar(requisicion);

            await CrearBitacora(
                idRequisicion,
                17,
                idUsuario,
                string.IsNullOrWhiteSpace(observaciones)
                    ? "Autorizada por Jefe de Áreas. Enviada a proceso de pago."
                    : observaciones.Trim());

            return true;
        }

        public async Task<bool> AutorizarConsolidada(int idConsolidada, int idUsuario, string? observaciones)
        {
            return await ActualizarConsolidada(
                idConsolidada,
                17,
                idUsuario,
                observaciones,
                "Autorizada por Jefe de Áreas. Enviada a proceso de pago.");
        }

        public async Task<bool> SolicitarModificacionRequisicion(int idRequisicion, int idUsuario, string observaciones)
        {
            var requisicion = await _repoRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 18;
            requisicion.FechaModificacion = DateTime.Now;
            await _repoRequisicion.Editar(requisicion);

            await CrearBitacora(idRequisicion, 18, idUsuario, observaciones.Trim());
            return true;
        }

        public async Task<bool> SolicitarModificacionConsolidada(int idConsolidada, int idUsuario, string observaciones)
        {
            return await ActualizarConsolidada(idConsolidada, 18, idUsuario, observaciones, observaciones);
        }

        public async Task<bool> RechazarRequisicion(int idRequisicion, int idUsuario, string motivo)
        {
            var requisicion = await _repoRequisicion.Obtener(r => r.IdRequisicion == idRequisicion);
            if (requisicion == null) return false;

            requisicion.IdEstatus = 5;
            requisicion.FechaModificacion = DateTime.Now;
            await _repoRequisicion.Editar(requisicion);

            await CrearBitacora(idRequisicion, 5, idUsuario, motivo.Trim());
            return true;
        }

        public async Task<bool> RechazarConsolidada(int idConsolidada, int idUsuario, string motivo)
        {
            return await ActualizarConsolidada(idConsolidada, 5, idUsuario, motivo, motivo);
        }

        private async Task<bool> ActualizarConsolidada(
            int idConsolidada,
            int nuevoEstatus,
            int idUsuario,
            string? observaciones,
            string observacionDefault)
        {
            var consolidada = await _repoConsolidada.Obtener(c => c.ConsolidadaId == idConsolidada);
            if (consolidada == null) return false;

            consolidada.IdEstatus = nuevoEstatus;
            consolidada.FechaModificacion = DateTime.Now;
            await _repoConsolidada.Editar(consolidada);

            var detallesQuery = await _repoConsolidadaDetalle.Consultar(d => d.ConsolidadaId == idConsolidada);
            var hijas = await detallesQuery
                .Select(d => d.IdRequisicionNavigation)
                .ToListAsync();

            var nota = string.IsNullOrWhiteSpace(observaciones)
                ? observacionDefault
                : observaciones.Trim();

            foreach (var hija in hijas)
            {
                hija.IdEstatus = nuevoEstatus;
                hija.FechaModificacion = DateTime.Now;
                await _repoRequisicion.Editar(hija);

                await CrearBitacora(
                    hija.IdRequisicion,
                    nuevoEstatus,
                    idUsuario,
                    $"[CONSOLIDADA {consolidada.FolioConsolidada}] {nota}");
            }

            return true;
        }

        private async Task CrearBitacora(int idRequisicion, int idEstatus, int idUsuario, string observacion)
        {
            await _repoBitacora.Crear(new TblBitacoraEstatus
            {
                IdRequisicion = idRequisicion,
                IdEstatus = idEstatus,
                FechaEstatus = DateTime.Now,
                Observacion = observacion,
                IdUsuario = idUsuario
            });
        }
    }
}
