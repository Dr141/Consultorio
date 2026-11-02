using Consultorio.Aplicacao.Interfaces;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Identity.Infraestrutura.Contexto;
using Consultorio.Identity.Infraestrutura.Servicos;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Server.IoC;

public static class RegistrarDependencias
{
    /// <summary>
    /// Método para registrar os serviços no contêiner de DI
    /// </summary>
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

        // Identity Service
        services.AddScoped<IIdentityService, IdentityService>();

        // Repositórios de Domínio / Infraestrutura
        services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
        services.AddScoped<IPessoaRepositorio, PessoaRepositorio>();
        services.AddScoped<IPacienteRepositorio, PacienteRepositorio>();
        services.AddScoped<IMedicoRepositorio, MedicoRepositorio>();
        services.AddScoped<IAgendaRepositorio, AgendaRepositorio>();
        services.AddScoped<IConsultaRepositorio, ConsultaRepositorio>();
        services.AddScoped<IExameRepositorio, ExameRepositorio>();

        // Serviços de Aplicação (DDD)
        services.AddScoped<IPessoaAppService, PessoaAppService>();
        services.AddScoped<IPacienteAppService, PacienteAppService>();
        services.AddScoped<IMedicoAppService, MedicoAppService>();
        services.AddScoped<IAgendaAppService, AgendaAppService>();
        services.AddScoped<IConsultaAppService, ConsultaAppService>();
        services.AddScoped<IExameAppService, ExameAppService>();
    }

    /// <summary>
    /// Método para rodar as migrações automaticamente quando configurado
    /// </summary>
    public static void Migrations(this IServiceProvider service, IConfiguration configuration)
    {
        bool runMigration = configuration.GetValue<bool>("Migration:RunMigration", false) ||
                            configuration.GetValue<bool>("RunMigration", false);

        if (!runMigration) return;

        using var scope = service.CreateScope();
        var dbAppIdentity = scope.ServiceProvider.GetRequiredService<IdentityContexto>();
        dbAppIdentity.Database.Migrate();

        var dbAppConsultorio = scope.ServiceProvider.GetRequiredService<ConsultorioContexto>();
        dbAppConsultorio.Database.Migrate();
    }
}
