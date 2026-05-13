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
        private readonly IGenericRepository<TblAdjudicacion> _repositoryAdquisicion;

        public ProveedoresService(
            IGenericRepository<TblProvedor> repository,
            IGenericRepository<TblCotizacione> repositoryCotizaciones,
            IGenericRepository<TblRequisicionDetalle> repositoryDetalle,
            IGenericRepository<TblRequisicionDetalleMovimiento> repositoryMovimiento,
            IGenericRepository<TblRequisicion> repositoryRequisicion,
            IGenericRepository<TblProveedorGanador> repositoryGanador,
            IGenericRepository<TblAdjudicacion> repositoryAdquisicion
        )
        {
            _repository = repository;
            _repositoryCotizaciones = repositoryCotizaciones;
            _repositoryDetalle = repositoryDetalle;
            _repositoryMovimiento = repositoryMovimiento;
            _repositoryRequisicion = repositoryRequisicion;
            _repositoryGanador = repositoryGanador;
            _repositoryAdquisicion = repositoryAdquisicion;
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
            var idsPartidasValidas = await (await _repositoryDetalle.Consultar(d => d.IdRequisicion == idRequisicion))
                .Select(d => d.IdRequisicionDetalle)
                .ToListAsync();

            var idsValidos = idsPartidasValidas.ToHashSet();
            var cotizacionesValidas = cotizaciones
                .Where(c => c.IdProveedor > 0 && c.IdPartida.HasValue && idsValidos.Contains(c.IdPartida.Value))
                .ToList();

            var tienePartidasInvalidas = cotizaciones.Any(c => c.IdPartida.HasValue && !idsValidos.Contains(c.IdPartida.Value));
            if (tienePartidasInvalidas)
                throw new InvalidOperationException("Hay partidas que no pertenecen a esta requisición.");

            // Eliminar cotizaciones anteriores de esta requisición
            var queryPrev = await _repositoryCotizaciones.Consultar(c => c.IdRequisicion == idRequisicion);
            var previas = await queryPrev.ToListAsync();
            foreach (var p in previas)
                await _repositoryCotizaciones.Eliminar(p);

            // Insertar las nuevas
            foreach (var cot in cotizacionesValidas)
            {
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
                NumPartida = c.IdRequiDetalleNavigation.NumPartida,
                NombrePartida = c.IdRequiDetalleNavigation.DescripcionDetallada,
                Descripcion = c.IdRequiDetalleNavigation.Descripcion,
                DescripcionDetallada = c.IdRequiDetalleNavigation.DescripcionDetallada,
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
                NombrePartida = c.DescripcionDetallada,
                Descripcion = c.Descripcion,
                DescripcionDetallada = c.DescripcionDetallada
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
            bool seleccionManual, string? justificacion, int idUsuario)
        {
            var queryPrev = await _repositoryGanador.Consultar(g => g.IdRequisicion == idRequisicion);
            var previo = await queryPrev.FirstOrDefaultAsync();

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

            var justificacionFinal = seleccionManual
                ? (justificacion ?? "").Trim()
                : null;
            var usuarioFinal = seleccionManual ? idUsuario : (int?)null;

            if (previo == null)
            {
                await _repositoryGanador.Crear(new TblProveedorGanador
                {
                    IdRequisicion = idRequisicion,
                    IdProveedor = idProveedor,
                    Subtotal = subtotal,
                    Iva = iva,
                    Total = subtotal + iva,
                    SeleccionManual = seleccionManual,
                    Justificacion = justificacionFinal,
                    IdUsuario = usuarioFinal,
                    FechaSeleccion = DateTime.Now
                });
            }
            else
            {
                previo.IdProveedor = idProveedor;
                previo.Subtotal = subtotal;
                previo.Iva = iva;
                previo.Total = subtotal + iva;
                previo.SeleccionManual = seleccionManual;
                previo.Justificacion = justificacionFinal;
                previo.IdUsuario = usuarioFinal;
                previo.FechaSeleccion = DateTime.Now;

                await _repositoryGanador.Editar(previo);
            }

            // Buscar el tipo de adquisición según el subtotal (sin IVA)
            var queryAdq = await _repositoryAdquisicion.Consultar(
                a => a.Activo == true
                  && a.MontoMin <= subtotal
                  && subtotal < a.MontoMax);
            var tipoAdq = await queryAdq.FirstOrDefaultAsync();

            // Actualizar la requisición con el IdAdquisicion encontrado
            var queryReq = await _repositoryRequisicion.Consultar(r => r.IdRequisicion == idRequisicion);
            var requisicion = await queryReq.FirstOrDefaultAsync();

            if (requisicion != null && tipoAdq != null)
            {
                requisicion.IdAdjudicacion = tipoAdq.Id;
                await _repositoryRequisicion.Editar(requisicion);
            }

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
                SeleccionManual = ganador.SeleccionManual,
                Justificacion = ganador.Justificacion ?? ""
            };
        }

        public async Task<List<OpcionProveedorDTO>> ObtenerOpcionesGanadorConsolidada(List<int?> idsRequisiciones)
        {
            // Traer detalles de TODAS las requisiciones y sumar cantidades por artículo+proveedor
            var queryDet = await _repositoryDetalle.Consultar(
                d => idsRequisiciones.Contains(d.IdRequisicion));
            var detalles = await queryDet.ToListAsync();

            // Cantidad total por idRequisicionDetalle
            var cantidadPorPartida = detalles.ToDictionary(
                d => d.IdRequisicionDetalle,
                d => d.Cantidad.HasValue ? (decimal)d.Cantidad.Value : 1m);

            // Cotizaciones de TODAS las requisiciones
            var queryCot = await _repositoryCotizaciones.Consultar(
                c => idsRequisiciones.Contains(c.IdRequisicion)
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

            // Agrupar por proveedor y sumar (importe × cantidad) de todas las partidas/requisiciones
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
    }
}
