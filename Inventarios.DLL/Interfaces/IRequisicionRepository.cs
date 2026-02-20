using Inventario.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.DAL.Interfaces
{
    public interface IRequisicionRepository : IGenericRepository<TblRequisicion>
    {
        Task<TblRequisicion> CrearConFolio(TblRequisicion requisicion);
    }
}
