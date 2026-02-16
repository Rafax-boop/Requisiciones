using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inventario.BLL.DTO;
using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;

namespace Inventario.BLL.Implementacion
{
    public class RequisicionService : IRequisicionesService
    {
        private readonly IGenericRepository<TblRequisicion> _repositoryRequisicion;
        private readonly IGenericRepository<TblRequisicionDetalle> _repositoryRequisicionDetalle;

        public RequisicionService(IGenericRepository<TblRequisicion> repositoryRequisicion, IGenericRepository<TblRequisicionDetalle> repositoryRequisicionDetalle)
        {
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryRequisicionDetalle = repositoryRequisicionDetalle;
        }

        public async Task<bool> CrearRequisicion(FormularioRequisicionDTO modelo)
        {
            var requisicion = new TblRequisicion
            {
                IdProvedor = modelo.IdProvedor,
                NumRequisicion = modelo.NumRequisicion,
                FechaEmision = DateTime.Now.Date,
                IdDepartamento = modelo.IdDepartamento,
                NomResponsableDepartamento = modelo.NomResponsableDepartamento,
                Domicilio = modelo.Domicilio,
                Telefono = modelo.Telefono,
                LugarEntrega = modelo.LugarEntrega,
                UsoEspecifico = modelo.UsoEspecifico,
                Justificacion = modelo.Justificacion,
                IdPeriodo = modelo.IdPeriodo,
                CuentaProgramaPresupuestario = modelo.CuentaProgramaPresupuestario,
                IdPrioridad = modelo.IdPrioridad,
                IdUsuario = modelo.IdUsuario,
                IdEstatus = modelo.IdEstatus,
                Activo = true,
                FechaSistema = DateTime.Now
            };
            var requiCreada = await _repositoryRequisicion.Crear(requisicion);

            var listaArticulos = new List<TblRequisicionDetalle>();

            foreach (var item in modelo.Articulos)
            {
                var detalle = new TblRequisicionDetalle
                {
                    IdRequisicion = requiCreada.IdRequisicion,
                    NumPartida = item.Cog,
                    IdArticulo = item.IdArticulo,
                    Cantidad = item.Cantidad,
                    UnidadMedida = item.UnidadMedida,
                    Descripcion = item.Descripcion,
                    FechaRegistro = DateTime.Now.Date
                };
                listaArticulos.Add(detalle);
            }

            await _repositoryRequisicionDetalle.CrearRango(listaArticulos);

            return true;
        }
    }
}
