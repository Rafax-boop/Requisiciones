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
        Task<DetallesRequiDTO> ObtenerDetallePorIdMaestro(int idMaestro);
        Task<RequisicionCompletaDTO?> ObtenerRequisicionCompletaPorId(int idRequisicion);
        Task<bool> ActualizarRequisicion(int idRequisicion, FormularioRequisicionDTO modelo, int idUsuario);
        Task<bool> AsignarRequisicion(int idRequi, int idUsuario,int idUsuarioMat);
        Task<bool> AtenderRequisicion(int idRequisicion, string observaciones, bool requiereModificacion, int idUsuario, int idpp, string FF, string tipoPrograma,
            int claveRegion, List<(int IdArticulo, int Cog)> cogsEditados = null);
        Task<string?> ObtenerObservacionesModificacion(int idRequisicion);
        Task<List<ProgresoPasoDTO>> ObtenerProgresoRequisicion(int idRequisicion);

        //metodos para las iamgenes de los diseños
        Task<bool> GuardarFotosRequisicion(int idRequisicion, List<IFormFile> fotos, string webRootPath);
        Task<List<string>> ObtenerFotosRequisicion(int idRequisicion);
        Task<bool> EliminarFotoRequisicion(int idFoto);
        Task<TblRegistroDiseno?> ObtenerFotoPorId(int idFoto);

        Task<List<TblRegistroDiseno>> ObtenerFotosConIdRequisicion(int idRequisicion);
    }
}
