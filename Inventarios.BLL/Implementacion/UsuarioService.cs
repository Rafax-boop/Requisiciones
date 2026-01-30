using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class UsuarioService : IUsuarioService
    {
        private readonly IGenericRepository<TblUsuario> _repositorio;

        public UsuarioService(IGenericRepository<TblUsuario> repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<TblUsuario> Crear(TblUsuario entidad)
        {
            try
            {
                TblUsuario usuarioCreado = await _repositorio.Crear(entidad);

                if (usuarioCreado == null || usuarioCreado.IdUsuario == 0)
                {
                    throw new InvalidOperationException("No se pudo crear el usuario");
                }

                return usuarioCreado;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<TblUsuario> Editar(TblUsuario entidad)
        {
            try
            {
                IQueryable<TblUsuario> queryUsuario = await _repositorio.Consultar(u => u.IdUsuario == entidad.IdUsuario);

                TblUsuario usuarioEditar = queryUsuario.First();
                usuarioEditar.Usuario = entidad.Usuario;
                usuarioEditar.Pasword = entidad.Pasword;
                usuarioEditar.NombreEnlace = entidad.NombreEnlace;
                usuarioEditar.CargoEnlace = entidad.CargoEnlace;
                usuarioEditar.Correoenlace = entidad.Correoenlace;
                usuarioEditar.Area = entidad.Area;
                usuarioEditar.IdRol = entidad.IdRol;
                usuarioEditar.Activo = entidad.Activo;

                bool respuesta = await _repositorio.Editar(usuarioEditar);

                if (!respuesta)
                {
                    throw new InvalidOperationException("No se pudo editar el usuario");
                }
                return usuarioEditar;
            }
            catch
            {
                throw;
            }
        }

        public async Task<bool> Eliminar(int idUsuario)
        {
            try
            {
                TblUsuario usuario = await _repositorio.Obtener(u => u.IdUsuario == idUsuario);

                bool eliminado = await _repositorio.Eliminar(usuario);
                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<TblUsuario>> Lista()
        {
            IQueryable<TblUsuario> query = await _repositorio.Consultar();
            return query.ToList();
        }

        public async Task<TblUsuario> ObtenerPorCredenciales(string usuario, string contrasena)
        {
            TblUsuario usuarioEncontrado = await _repositorio.Obtener(u => u.Usuario == usuario && u.Pasword == contrasena);

            return usuarioEncontrado;
        }
    }
}
