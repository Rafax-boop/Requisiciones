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


    public class CatalogoService : ICatalogoService
    {
        private readonly IGenericRepository<TblMunicipio> _repositoryMunicipios;
        private readonly IGenericRepository<TblProgramaPresupuestario> _repositoryPP;
        private readonly IGenericRepository<TblFuentesFinanciamiento> _repositoryFuentesFinanciamiento;

        public CatalogoService(IGenericRepository<TblMunicipio> repositoryMunicipios, IGenericRepository<TblProgramaPresupuestario> repositoryPP, IGenericRepository<TblFuentesFinanciamiento> repositoryFuentesFinanciamiento)
        {
            _repositoryMunicipios = repositoryMunicipios;
            _repositoryPP = repositoryPP;
            _repositoryFuentesFinanciamiento = repositoryFuentesFinanciamiento;
        }

        public async Task<List<CatalogoDTO>> ObtenerActividades()
        {
            var query = await _repositoryPP.Consultar();

            var actividades = await query
                .GroupBy(p => p.DescripcionActividad)
                .Select(g => new CatalogoDTO
                {
                    Id = g.Min(x => x.Id),
                    Nombre = g.Key
                })
                .ToListAsync();

            return actividades;
        }

        public async Task<List<CatalogoDTO>> ObtenerFuentesFinanciamiento()
        {
            var query = await _repositoryFuentesFinanciamiento.Consultar();

            var fuentes = await query
                .Select(p => new CatalogoDTO
                {
                    Clave = p.Clave,
                    Nombre = p.FuenteFinanciamiento
                })
                .Distinct()
                .ToListAsync();

            return fuentes;
        }

        public async Task<List<CatalogoDTO>> ObtenerMunicipios()
        {
            var query = await _repositoryMunicipios.Consultar();

            var municipios = await query
                .Select(p => new CatalogoDTO
                {
                    Id = p.IdMunicipio,
                    Nombre = p.NombreMunicipios
                })
                .Distinct()
                .ToListAsync();

            return municipios;
        }
    }
}
