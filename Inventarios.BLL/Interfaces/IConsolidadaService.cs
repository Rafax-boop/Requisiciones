using Inventario.BLL.DTO;
using Inventario.Entity;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IConsolidadaService
    {
        Task<List<RequisicionMaestraDTO>> ObtenerRequisicionesConsolidables();
        Task<TblConsolidada> CrearConsolidada(List<int> idsRequisiciones, int idUsuario, bool servicio);
        Task<List<ConsolidadaDTO>> ListarConsolidadas(bool servicio, int? idUsuario = null);
        Task<ConsolidadaDetalleDTO> ObtenerDetalleConsolidada(int idConsolidada);
        Task<bool> AtenderConsolidada(AtenderConsolidadaDTO modelo, int idUsuario);
        Task<bool> GuardarArchivosAtencionConsolidada(int idConsolidada,
            List<IFormFile> cuadroComparativo, List<IFormFile> anexos, string webRootPath);
        Task<List<PartidaConsolidadaDTO>> ObtenerPartidasConsolidada(int idConsolidada);
        Task<TblConsolidada?> ObtenerConsolidada(int idConsolidada);

        Task<List<ConsolidadaVerificadaDTO>> ObtenerConsolidadasVerificadas(int idUsuario, bool servicio);
        Task<ConsolidadaExpedienteDTO> ObtenerExpedienteConsolidada(int idConsolidada);
        Task<bool> SubirDocumentoProveedorConsolidada(int idConsolidada, string tipoDocumento,
            IFormFile archivo, string webRootPath, int idUsuario);
        Task<List<ArchivoAtencionDTO>> ObtenerDocumentosProveedorConsolidada(int idConsolidada);
        Task<List<ConsolidadaDTO>> ObtenerConsolidadasConArchivos(bool? servicio = null, int? idUsuarioMat = null, int? idUsuarioFinan = null);
        Task<List<ArchivoRequisicionDTO>> ObtenerArchivosConsolidada(int idConsolidada);
        Task<bool> AsignarAnalistaConsolidada(int idConsolidada, int idUsuario, int idUsuarioAsignador);
        Task<List<ProgresoPasoDTO>> ObtenerProgresoConsolidada(int idConsolidada);
    }
}
