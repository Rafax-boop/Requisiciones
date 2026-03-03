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
    public class MunicipioService :  IMunicipioServie
    {
        private readonly IGenericRepository<TblMunicipio> _repository;

        public MunicipioService(IGenericRepository<TblMunicipio> repository)
        {
            _repository = repository;
        }
        public async Task<List<MunicipiosDTO>> ObtenerMunicipios()
        {
            var query = await _repository.Consultar();

            var municipios = await query
                .Select(p => new MunicipiosDTO
                {
                    Id = p.IdMunicipio,
                    Municipio = p.NombreMunicipios
                })
                .Distinct()
                .ToListAsync();

            return municipios;
        }
    }
}
