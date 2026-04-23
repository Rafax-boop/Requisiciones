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
    public class ProveedoresService : IProveedoresService
    {
        private readonly IGenericRepository<TblProvedor> _repository;
        private readonly IGenericRepository<TblCotizacione> _repositoryCotizaciones;
        private readonly IGenericRepository<TblRequisicionDetalle> _repositoryDetalle;
        private readonly IGenericRepository<TblRequisicionDetalleMovimiento> _repositoryMovimiento;
        private readonly IGenericRepository<TblRequisicion> _repositoryRequisicion;

        public ProveedoresService(
            IGenericRepository<TblProvedor> repository,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblRequisicionDetalle> repositoryDetalle,
            IGenericRepository<TblRequisicionDetalleMovimiento> repositoryMovimiento,
            IGenericRepository<TblRequisicion> repositoryRequisicion
        )
        {
            _repository = repository;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repositoryDetalle = repositoryDetalle;
            _repositoryMovimiento = repositoryMovimiento;
            _repositoryRequisicion = repositoryRequisicion;
        }

        public async Task<List<ProveedoresDTO>> ObtenerProveedores()
        {
            var query = await _repository.Consultar();

            var proveedores = await query
                .Select(p => new ProveedoresDTO
                {
                    Id = p.IdProvedor,
                    Proveedor = p.NombreProvedor
                })
                .Distinct()
                .ToListAsync();

            return proveedores;
        }

        public async Task<bool> GuardarCotizaciones(int idRequisicion, List<CotizacionDTO> cotizaciones)
        {
            // Eliminar cotizaciones anteriores de esta requisición
            var queryPrev = await _repositoryCotizaciones.Consultar(c => c.IdRequisicion == idRequisicion);
            var previas = await queryPrev.ToListAsync();
            foreach (var p in previas)
                await _repositoryCotizaciones.Eliminar(p);

            // Insertar las nuevas
            foreach (var cot in cotizaciones)
            {
                if (cot.IdProveedor <= 0) continue;
                await _repositoryCotizaciones.Crear(new TblCotizacione
                {
                    IdRequisicion = idRequisicion,
                    IdProveedor = cot.IdProveedor,
                    Importe = cot.Importe,
                    IdRequiDetalle = cot.IdPartida,
                    Iva = cot.IVA
                });
            }
            return true;
        }

        public async Task<List<CotizacionDTO>> ObtenerCotizaciones(int idRequisicion)
        {
            var query = await _repositoryCotizaciones.Consultar(c => c.IdRequisicion == idRequisicion);
            return await query.Select(c => new CotizacionDTO
            {
                IdProveedor = c.IdProveedor ?? 0,
                Importe = c.Importe ?? 0,
                NombreProveedor = c.IdProveedorNavigation.NombreProvedor,
                IdPartida = c.IdRequiDetalle,
                NombrePartida = c.IdRequiDetalleNavigation.DescripcionDetallada,
                IVA = c.Iva
            }).ToListAsync();
        }

        public async Task<List<PartidaDTO>> ObtenerPartidas(int idRequisicion)
        {
            // Verificar si es requisición de servicio
            var queryMaestra = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);
            var maestra = await queryMaestra.FirstOrDefaultAsync();
            bool esServicio = maestra?.RequiServicio ?? false;

            var query = await _repositoryDetalle.Consultar(c => c.IdRequisicion == idRequisicion);

            if (!esServicio)
            {
                var queryMovimientos = await _repositoryMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion && m.TipoMovimiento == "COMPRA");
                var idsParaCompra = await queryMovimientos
                    .Select(m => m.IdRequisicionDetalle)
                    .Distinct()
                    .ToListAsync();

                if (idsParaCompra.Any())
                    query = query.Where(r => idsParaCompra.Contains(r.IdRequisicionDetalle));
            }

            return await query.Select(c => new PartidaDTO
            {
                IdRequiDetalle = c.IdRequisicionDetalle,
                NumPartida = c.NumPartida,
                NombrePartida = c.DescripcionDetallada
            }).ToListAsync();
        }

        public async Task<List<DetalleArticuloDTO>> ObtenerArticulosParaCompra(int idRequisicion)
        {
            var queryMaestra = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);
            var maestra = await queryMaestra.FirstOrDefaultAsync();
            bool esServicio = maestra?.RequiServicio ?? false;

            var query = await _repositoryDetalle.Consultar(r => r.IdRequisicion == idRequisicion);

            if (!esServicio)
            {
                var queryMovimientos = await _repositoryMovimiento.Consultar(
                    m => m.IdRequisicion == idRequisicion && m.TipoMovimiento == "COMPRA");
                var idsParaCompra = await queryMovimientos
                    .Select(m => m.IdRequisicionDetalle)
                    .Distinct()
                    .ToListAsync();

                if (idsParaCompra.Any())
                    query = query.Where(r => idsParaCompra.Contains(r.IdRequisicionDetalle));
            }

            return await query.Select(r => new DetalleArticuloDTO
            {
                IdRequisicionDetalle = r.IdRequisicionDetalle,
                NumPartida = r.NumPartida,
                IdArticulo = r.IdArticulo,
                Cantidad = r.Cantidad,
                UnidadMedida = r.UnidadMedida,
                Descripcion = r.Descripcion,
                DescripcionDetallada = r.DescripcionDetallada
            }).ToListAsync();
        }
    }
}
