using Inventario.BLL.Implementacion;
using Inventario.BLL.Interfaces;
using Inventario.DAL.DBCONTEXT;
using Inventario.DAL.Implementacion;
using Inventario.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inventario.IOC
{
    public static class Dependencia
    {
        public static void InyectarDependencias(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<DbSigereContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("CadenaSQL")));

            services.AddTransient(typeof(IGenericRepository<>), typeof(GenericRepository<>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IRequisicionRepository, RequisicionRepository>();
            services.AddScoped<IUsuarioService, UsuarioService>();
            services.AddScoped<IRequisicionesService, RequisicionService>();
            services.AddScoped<ICuadroComparativoPdfService, CuadroComparativoPdfService>();
            services.AddScoped<IArticulosService, ArticulosService>();
            services.AddScoped<IAlmacenService, AlmacenService>();
            services.AddScoped<IMunicipioServie, MunicipioService>();
            services.AddScoped<IProgramaPresupuestarioService, ProgramaPresupuestarioService>();
            services.AddScoped<IFinancierosService, FinancierosService>();
            services.AddScoped<IDAFService, DAFService>();
            services.AddScoped<IProveedoresService, ProveedoresService>();
            services.AddScoped<IEmailService, EmailService>();
        }
    }
}
