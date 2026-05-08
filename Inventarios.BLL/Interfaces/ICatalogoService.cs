using Inventario.BLL.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Interfaces
{
    public interface ICatalogoService
    {
        Task<List<CatalogoDTO>> ObtenerMunicipios();
        Task<List<CatalogoDTO>> ObtenerActividades();
        Task<List<CatalogoDTO>> ObtenerFuentesFinanciamiento();
    }
}
