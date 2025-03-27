using Consultorio.Identity.Infraestrutura.Contexto;
using Consultorio.Identity.Infraestrutura.Servicos;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Interfaces.Base;
using Consultorio.Infraestrutura.Repositorios.Base;
using Consultorio.Modelo.Entidades;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Server.IoC;

public static class RegistrarDependencias
{
    /// <summary>
    /// Método para registrar os serviços
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityContexto>(options =>
            options.UseNpgsql(configuration.GetConnectionString("IdentityConection"))
        );
        services.AddDbContext<ConsultorioContexto>(options =>
            options.UseNpgsql(configuration.GetConnectionString("ConsultorioConection"))
        );
        services.AddDefaultIdentity<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContexto>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRepositorio<Consulta>, Repositorio<Consulta>>();
        services.AddScoped<IRepositorio<Exame>, Repositorio<Exame>>();
        services.AddScoped<IRepositorio<Medico>, Repositorio<Medico>>();
        services.AddScoped<IRepositorio<Paciente>, Repositorio<Paciente>>();
        services.AddScoped<IRepositorio<Pessoa>, Repositorio<Pessoa>>();
    }

    /// <summary>
    /// Método para rodar as migrações
    /// </summary>
    /// <param name="service"></param>
    /// <param name="configuration"></param>
    public static void Migrations(this IServiceProvider service, IConfiguration configuration)
    {
        bool runMigration = configuration.GetValue<bool>("RunMigration");
        if (!runMigration) return;

        using var scope = service.CreateScope();        
        var dbAppIdentity = scope.ServiceProvider.GetRequiredService<IdentityContexto>();
        dbAppIdentity.Database.Migrate();
        var dbAppConsultorio = scope.ServiceProvider.GetRequiredService<ConsultorioContexto>();
        dbAppConsultorio.Database.Migrate();        
    }
}
