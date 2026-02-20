using Inventario.BLL.DTO;
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
        private readonly IGenericRepository<TblUsuario> _repositorioUsuarios;
        private readonly IGenericRepository<TblDepartamento> _repositorioDepartamentos;

        public UsuarioService(IGenericRepository<TblUsuario> repositorio, IGenericRepository<TblDepartamento> repositorioDepartamentos)
        {
            _repositorioUsuarios = repositorio;
            _repositorioDepartamentos = repositorioDepartamentos;
        }

        public async Task<TblUsuario> Crear(TblUsuario entidad)
        {
            try
            {
                TblUsuario usuarioCreado = await _repositorioUsuarios.Crear(entidad);

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
                IQueryable<TblUsuario> queryUsuario = await _repositorioUsuarios.Consultar(u => u.IdUsuario == entidad.IdUsuario);

                TblUsuario usuarioEditar = queryUsuario.First();
                usuarioEditar.Usuario = entidad.Usuario;
                usuarioEditar.Pasword = entidad.Pasword;
                usuarioEditar.Telefono = entidad.Telefono;
                usuarioEditar.Correo = entidad.Correo;
                usuarioEditar.Area = entidad.Area;
                usuarioEditar.IdRol = entidad.IdRol;
                usuarioEditar.Activo = entidad.Activo;

                bool respuesta = await _repositorioUsuarios.Editar(usuarioEditar);

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
                TblUsuario usuario = await _repositorioUsuarios.Obtener(u => u.IdUsuario == idUsuario);

                bool eliminado = await _repositorioUsuarios.Eliminar(usuario);
                return true;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<TblUsuario>> Lista()
        {
            IQueryable<TblUsuario> query = await _repositorioUsuarios.Consultar();
            return query.ToList();
        }

        public async Task<FormularioRequisicionDTO> ObtenerDatosDepartamento(int? idUsuario)
        {
            var usuario = await _repositorioUsuarios.Obtener(u => u.IdUsuario == idUsuario);

            var departamento = await _repositorioDepartamentos
                .Obtener(d => d.IdDepartamento == usuario.Area);

            return new FormularioRequisicionDTO
            {
                IdDepartamento = departamento.IdDepartamento,
                Departamento = departamento.NombreDepartamento,
                Correo = usuario.Correo,
                Telefono = usuario.Telefono,
                NomResponsableDepartamento = departamento.NombreJefe,
                NomDirector = departamento.NombreDirector
            };
        }

        public async Task<TblUsuario> ObtenerPorCredenciales(string usuario, string contrasena)
        {
            TblUsuario usuarioEncontrado = await _repositorioUsuarios.Obtener(u => u.Usuario == usuario && u.Pasword == contrasena);

            return usuarioEncontrado;
        }
    }
}
