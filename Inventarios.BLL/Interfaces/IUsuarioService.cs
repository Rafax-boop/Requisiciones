using Inventario.BLL.DTO;
using Inventario.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IUsuarioService
    {
        Task<TblUsuario> ObtenerPorCredenciales(string usuario, string contrasena);
        Task<List<TblUsuario>> Lista();
        Task<List<TblUsuario>> ListaUsuariosAsignar(int idRol);
        //Task<TblUsuario> Crear(TblUsuario entidad);
        //Task<TblUsuario> Editar(TblUsuario entidad);
        //Task<bool> Eliminar(int idUsuario);
        Task<FormularioRequisicionDTO> ObtenerDatosDepartamento(int? idUsuario);
    }
}
