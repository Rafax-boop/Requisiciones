using Inventario.BLL.Interfaces;
using Inventario.DAL.Interfaces;
using Inventario.Entity;
using Inventario.Entity.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.BLL.Implementacion
{
    public class ArticulosService : IArticulosService
    {
        private readonly IGenericRepository<TblArticulo> _repositoryArticulo;

        public ArticulosService(IGenericRepository<TblArticulo> repositoryArticulo)
        {
            _repositoryArticulo = repositoryArticulo;
        }

        public async Task<TblArticulo> ObtenerArticuloPorId(int idArticulo)
        {
            var articulo = await _repositoryArticulo.Obtener(a => a.Id == idArticulo);
            if (articulo != null)
                articulo.Descripcion = FixMojibake(articulo.Descripcion);
            return articulo;
        }

        public async Task<List<TblArticulo>> BuscarArticulos(string termino, TipoBusquedaArticulo tipo)
        {
            IQueryable<TblArticulo> query = await _repositoryArticulo.Consultar();

            switch (tipo)
            {
                case TipoBusquedaArticulo.Mensual:
                    query = query.Where(a => a.Cog >= 2000 && a.Cog < 3000);
                    break;

                case TipoBusquedaArticulo.Servicio:
                    query = query.Where(a => a.Cog >= 3000 && a.Cog < 4000);
                    break;

                case TipoBusquedaArticulo.Normal:
                default:
                    query = query.Where(a => !(a.Cog >= 3000 && a.Cog < 4000));
                    break;
            }

            if (!string.IsNullOrWhiteSpace(termino))
                query = query.Where(a => EF.Functions.Collate(a.Descripcion, "Latin1_General_CI_AI").Contains(termino));

            var resultados = await query.Take(10).ToListAsync();

            foreach (var a in resultados)
                a.Descripcion = FixMojibake(a.Descripcion);

            return resultados;
        }

        private static string FixMojibake(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;

            text = text.Replace("Ð", "Ñ")
                       .Replace("¥", "Ñ")
                       .Replace("à", "Ó")
                       .Replace("µ", "Á")
                       .Replace("é", "Ú")
                       .Replace("Ö", "Í");

            try
            {
                byte[] latin1 = Encoding.GetEncoding("ISO-8859-1").GetBytes(text);
                string utf8 = Encoding.UTF8.GetString(latin1);
                return utf8.Contains('\uFFFD') ? text : utf8;
            }
            catch
            {
                return text;
            }
        }

        public async Task<List<int>> BuscarCogs(string termino)
        {
            var query = await _repositoryArticulo.Consultar();

            if (!string.IsNullOrWhiteSpace(termino) && int.TryParse(termino, out int cogNum))
                query = query.Where(a => a.Cog.ToString().Contains(termino));
            else if (!string.IsNullOrWhiteSpace(termino))
                query = query.Where(a => a.Cog.ToString().Contains(termino));

            return await query
                .Select(a => a.Cog)
                .Distinct()
                .OrderBy(c => c)
                .Take(10)
                .ToListAsync();
        }
    }
}
