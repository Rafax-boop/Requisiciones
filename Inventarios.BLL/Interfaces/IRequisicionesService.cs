using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventario.BLL.DTO;
using Inventario.Entity;
using Microsoft.AspNetCore.Http;

namespace Inventario.BLL.Interfaces
{
    public interface IRequisicionesService
    {
        Task<TblRequisicion> CrearRequisicion(FormularioRequisicionDTO modelo, int idUsuario, bool servicio);
        Task<List<RequisicionMaestraDTO>> ListarRequisiciones(int? idDepartamento, bool servicio, int? idUsuarioMat = null);
        Task<List<RequisicionMaestraDTO>> ListarRequisicionesAutorizadas();
        Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro);
        Task<RequisicionCompletaDTO?> ObtenerRequisicionCompletaPorId(int idRequisicion);
        Task<bool> ActualizarRequisicion(int idRequisicion, FormularioRequisicionDTO modelo, int idUsuario);
        Task<bool> AsignarRequisicion(int idRequi, int idUsuario,int idUsuarioMat);
        Task<bool> AtenderRequisicion(AtenderRequiDTO modelo, int idUsuario);
        Task<string?> ObtenerObservacionesModificacion(int idRequisicion);
        Task<List<ProgresoPasoDTO>> ObtenerProgresoRequisicion(int idRequisicion);
        Task<bool> EnviarAAlmacen(int idRequisicion, int idUsuario);
        Task<bool> EnviarAModificacion(int idRequisicion, int idUsuario, string observaciones);
        Task<bool> RechazarRequisicion(int idRequisicion, int idUsuario, string motivo);
        Task<bool> AceptarExpediente(int idRequisicion, int idUsuario);

        //metodos para las iamgenes de los diseños
        Task<bool> GuardarFotosRequisicion(int idRequisicion, List<IFormFile> fotos, string webRootPath);
        Task<List<string>> ObtenerFotosRequisicion(int idRequisicion);
        Task<bool> EliminarFotoRequisicion(int idFoto);
        Task<TblRegistroDiseno?> ObtenerFotoPorId(int idFoto);
        Task<List<TblRegistroDiseno>> ObtenerFotosConIdRequisicion(int idRequisicion);

        Task<bool> GuardarArchivosAtencion(
            int idRequisicion,
            List<IFormFile> cotizaciones,
            List<IFormFile> cuadroComparativo,
            string webRootPath);
    }
}
