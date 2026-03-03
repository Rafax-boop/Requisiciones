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
    public class ProgramaPresupuestarioService : IProgramaPresupuestarioService
    {
        private readonly IGenericRepository<TblProgramaPresupuestario> _repository;

        public ProgramaPresupuestarioService(IGenericRepository<TblProgramaPresupuestario> repository)
        {
            _repository = repository;
        }
        public async Task<List<ActividadDTO>> ObtenerActividades()
        {
            var query = await _repository.Consultar();

            var actividades = await query
                .GroupBy(p => p.DescripcionActividad)
                .Select(g => new ActividadDTO
                {
                    Id = g.Min(x => x.Id), // Tomamos uno cualquiera
                    DescripcionActividad = g.Key
                })
                .ToListAsync();

            return actividades;
        }
    }
}
