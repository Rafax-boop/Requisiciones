using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IAlmacenService
    {
        Task<List<RequisicionMaestraDTO>> ListarRequisicionesAlmacen();
        Task<List<InventarioItemDTO>> ObtenerInventario();
        Task<List<string>> ObtenerEstatus();

        Task<(bool ok, string? error)> RegistrarIngresoInventario(
            string? clave,
            string descripcion,
            string unidadMedida,
            int cantidad,
            int idUsuario,
            string motivo);

        Task<(bool ok, string? error)> AprobarRequisicionCompleta(int idRequisicion, int idUsuario);

        Task<(bool ok, string? error)> AprobarRequisicionParcial(
            int idRequisicion,
            int idUsuario,
            IEnumerable<(int idRequisicionDetalle, int cantidadAprobada)> partidas);

        Task<(bool ok, string? error)> RechazarRequisicionAlmacen(int idRequisicion, int idUsuario, string motivo);

        Task<(bool ok, string? error)> AnularMovimientoInventario(int idMovimiento, int idUsuario, string motivoAnulacion);
    }
}
