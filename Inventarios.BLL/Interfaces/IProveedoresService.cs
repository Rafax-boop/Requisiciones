using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface IProveedoresService
    {
        Task<List<ProveedoresDTO>> ObtenerProveedores();
        Task<bool> GuardarCotizaciones(int idRequisicion, List<CotizacionDTO> cotizaciones);
        Task<List<CotizacionDTO>> ObtenerCotizaciones(int idRequisicion);
        Task<List<PartidaDTO>> ObtenerPartidas(int idRequisicion);
        Task<List<DetalleArticuloDTO>> ObtenerArticulosParaCompra(int idRequisicion);
    }
}
