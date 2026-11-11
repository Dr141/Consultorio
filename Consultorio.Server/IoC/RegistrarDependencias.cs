using Consultorio.Aplicacao.Interfaces;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Identity.Infraestrutura.Contexto;
using Consultorio.Identity.Infraestrutura.Servicos;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Consultorio.Infraestrutura.Cache;
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
        {
            options.UseNpgsql(configuration.GetConnectionString("IdentityConection"));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddDbContext<ConsultorioContexto>(options =>
        {
            options.UseNpgsql(configuration.GetConnectionString("ConsultorioConection"));
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });

        services.AddDefaultIdentity<IdentityUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<IdentityContexto>()
            .AddDefaultTokenProviders();

        // Identity Service
        services.AddScoped<IIdentityService, IdentityService>();

        // Cache Distribuído (Redis com fallback gracioso para memória)
        bool redisHabilitado = configuration.GetValue<bool>("Redis:Enabled", false);
        string? redisConn = configuration.GetValue<string>("Redis:ConnectionString");

        if (redisHabilitado && !string.IsNullOrWhiteSpace(redisConn))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConn;
                options.InstanceName = configuration.GetValue<string>("Redis:InstanceName", "Consultorio:");
            });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, RedisCacheService>();

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
        services.AddScoped<IProntuarioAppService, ProntuarioAppService>();
    }

    /// <summary>
    /// Método para rodar as migrações automaticamente quando configurado
    /// </summary>
    public static void Migrations(this IServiceProvider service, IConfiguration configuration)
    {
        bool runMigration = configuration.GetValue<bool>("Migration:RunMigration", false) ||
                            configuration.GetValue<bool>("RunMigration", false);

        if (!runMigration) return;

        try
        {
            EnsureDatabaseExists(configuration.GetConnectionString("IdentityConection"));
            EnsureDatabaseExists(configuration.GetConnectionString("ConsultorioConection"));

            using var scope = service.CreateScope();
            var dbAppIdentity = scope.ServiceProvider.GetRequiredService<IdentityContexto>();
            dbAppIdentity.Database.Migrate();

            var dbAppConsultorio = scope.ServiceProvider.GetRequiredService<ConsultorioContexto>();
            dbAppConsultorio.Database.Migrate();
            dbAppConsultorio.Database.ExecuteSqlRaw("""
                ALTER TABLE "Consulta" ALTER COLUMN "IdAgenda" DROP NOT NULL;
                ALTER TABLE "Exame" ALTER COLUMN "IdAgenda" DROP NOT NULL;
                ALTER TABLE "Exame" ALTER COLUMN "Resultado" DROP NOT NULL;
                ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "IdMedico" integer NULL REFERENCES "Medico"("Id") ON DELETE SET NULL;
            """);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AVISO] Migrações de banco de dados: {ex.Message}");
        }
    }

    private static void EnsureDatabaseExists(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        try
        {
            var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
            var targetDatabase = builder.Database;
            if (string.IsNullOrWhiteSpace(targetDatabase) || targetDatabase.Equals("postgres", StringComparison.OrdinalIgnoreCase))
                return;

            builder.Database = "postgres";
            using var conn = new Npgsql.NpgsqlConnection(builder.ConnectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1 FROM pg_database WHERE datname = @name";
            cmd.Parameters.AddWithValue("name", targetDatabase);
            var exists = cmd.ExecuteScalar() != null;

            if (!exists)
            {
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = $"CREATE DATABASE \"{targetDatabase}\"";
                createCmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AVISO] Verificação/Criação do banco de dados ({connectionString}): {ex.Message}");
        }
    }
}
