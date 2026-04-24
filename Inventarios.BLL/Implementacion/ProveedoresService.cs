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
        private readonly IGenericRepository<TblProveedorGanador> _repositoryGanador;

        public ProveedoresService(
            IGenericRepository<TblProvedor> repository,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblRequisicionDetalle> repositoryDetalle,
            IGenericRepository<TblRequisicionDetalleMovimiento> repositoryMovimiento,
            IGenericRepository<TblRequisicion> repositoryRequisicion,
            IGenericRepository<TblProveedorGanador> repositoryGanador
        )
        {
            _repository = repository;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repositoryDetalle = repositoryDetalle;
            _repositoryMovimiento = repositoryMovimiento;
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryGanador = repositoryGanador;
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

        public async Task<List<OpcionProveedorDTO>> ObtenerOpcionesGanador(int idRequisicion)
        {
            var queryDet = await _repositoryDetalle.Consultar(d => d.IdRequisicion == idRequisicion);
            var detalles = await queryDet.ToListAsync();

            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m);

            // ── Proyectar en BD (incluye join con proveedor) antes de ToListAsync ──
            var queryCot = await _repositoryCotizaciones.Consultar(
                c => c.IdRequisicion == idRequisicion
                  && c.IdRequiDetalle.HasValue
                  && c.Importe.HasValue
                  && c.IdProveedor.HasValue);

            var filas = await queryCot
                .Select(c => new
                {
                    c.IdProveedor,
                    NombreProveedor = c.IdProveedorNavigation.NombreProvedor,
                    c.IdRequiDetalle,
                    c.Importe,
                    c.Iva
                })
                .ToListAsync();

            // ── Agrupar en memoria (ya no hay navegación lazy que falle) ──
            var opciones = filas
                .GroupBy(c => new { c.IdProveedor, c.NombreProveedor })
                .Select(g =>
                {
                    var subtotal = g.Sum(c =>
                    {
                        var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                        return c.Importe!.Value * cant;
                    });
                    var iva = g.Sum(c =>
                    {
                        if (c.Iva != true) return 0m;
                        var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                        return c.Importe!.Value * cant * 0.16m;
                    });
                    return new OpcionProveedorDTO
                    {
                        IdProveedor = g.Key.IdProveedor ?? 0,
                        NombreProveedor = g.Key.NombreProveedor ?? "",
                        Subtotal = subtotal,
                        Iva = iva,
                        Total = subtotal + iva,
                        EsSugerido = false
                    };
                })
                .Where(o => o.IdProveedor > 0 && o.Total > 0)
                .OrderBy(o => o.Total)
                .ToList();

            if (opciones.Any())
                opciones.First().EsSugerido = true;

            return opciones;
        }

        public async Task<bool> GuardarProveedorGanador(int idRequisicion, int idProveedor,
            bool seleccionManual, int idUsuario)
        {
            // Eliminar ganador anterior si existe
            var queryPrev = await _repositoryGanador.Consultar(g => g.IdRequisicion == idRequisicion);
            var previo = await queryPrev.FirstOrDefaultAsync();
            if (previo != null)
                await _repositoryGanador.Eliminar(previo);

            // Calcular totales del proveedor elegido
            var queryCot = await _repositoryCotizaciones.Consultar(
                c => c.IdRequisicion == idRequisicion
                  && c.IdProveedor == idProveedor
                  && c.IdRequiDetalle.HasValue
                  && c.Importe.HasValue);
            var cotizaciones = await queryCot.ToListAsync();

            var queryDet = await _repositoryDetalle.Consultar(d => d.IdRequisicion == idRequisicion);
            var detalles = await queryDet.ToListAsync();
            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m);

            var subtotal = cotizaciones.Sum(c =>
            {
                var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                return c.Importe!.Value * cant;
            });
            var iva = cotizaciones.Sum(c =>
            {
                if (c.Iva != true) return 0m;
                var cant = cantidadPorPartida.TryGetValue(c.IdRequiDetalle!.Value, out var q) ? q : 1m;
                return c.Importe!.Value * cant * 0.16m;
            });

            await _repositoryGanador.Crear(new TblProveedorGanador
            {
                IdRequisicion = idRequisicion,
                IdProveedor = idProveedor,
                Subtotal = subtotal,
                Iva = iva,
                Total = subtotal + iva,
                SeleccionManual = seleccionManual,
                IdUsuario = seleccionManual ? idUsuario : null,
                FechaSeleccion = DateTime.Now
            });

            return true;
        }

        public async Task<ProveedorGanadorDTO?> ObtenerProveedorGanador(int idRequisicion)
        {
            var query = await _repositoryGanador.Consultar(g => g.IdRequisicion == idRequisicion);
            var ganador = await query.FirstOrDefaultAsync();
            if (ganador == null) return null;

            var proveedor = await _repository.Obtener(p => p.IdProvedor == ganador.IdProveedor);

            return new ProveedorGanadorDTO
            {
                IdProveedor = ganador.IdProveedor,
                NombreProveedor = proveedor?.NombreProvedor ?? "",
                Subtotal = ganador.Subtotal ?? 0,
                Iva = ganador.Iva ?? 0,
                Total = ganador.Total ?? 0,
                SeleccionManual = ganador.SeleccionManual
            };
        }
    }
}
