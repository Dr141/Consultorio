using Consultorio.Identity.Infraestrutura.Contexto;
using Consultorio.Identity.Infraestrutura.Servicos;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Server.IoC;

public static class RegistrarDependencias
{
    public static void RegisterServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityContexto>(options =>
            options.UseNpgsql(configuration.GetConnectionString("IdentityConection"))
        );

        services.AddDefaultIdentity<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContexto>()
            .AddDefaultTokenProviders();

        services.AddScoped<IIdentityService, IdentityService>();
    }

    public static void Migrations(this IServiceProvider service)
    {
        using (var scope = service.CreateScope())
        {
            var dbAppIdentity = scope.ServiceProvider.GetRequiredService<IdentityContexto>();
            dbAppIdentity.Database.Migrate();
        }
    }
}
