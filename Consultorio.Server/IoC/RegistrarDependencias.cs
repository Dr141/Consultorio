using Consultorio.Aplicacao.Interfaces;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Identity.Infraestrutura.Contexto;
using Consultorio.Identity.Infraestrutura.Servicos;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Consultorio.Infraestrutura.Cache;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Notificacoes;
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
        services.AddScoped<IConsultaAdendoRepositorio, ConsultaAdendoRepositorio>();
        services.AddScoped<IAuditoriaRepositorio, AuditoriaRepositorio>();
        services.AddScoped<IBloqueioAgendaRepositorio, BloqueioAgendaRepositorio>();
        services.AddScoped<IDocumentoClinicoRepositorio, DocumentoClinicoRepositorio>();
        services.AddScoped<IExameAnexoRepositorio, ExameAnexoRepositorio>();

        // Serviços de Aplicação (DDD)
        services.AddScoped<IPessoaAppService, PessoaAppService>();
        services.AddScoped<IPacienteAppService, PacienteAppService>();
        services.AddScoped<IMedicoAppService, MedicoAppService>();
        services.AddScoped<IAgendaAppService, AgendaAppService>();
        services.AddScoped<IConsultaAppService, ConsultaAppService>();
        services.AddScoped<IExameAppService, ExameAppService>();
        services.AddScoped<IProntuarioAppService, ProntuarioAppService>();
        services.AddScoped<IAuditoriaAppService, AuditoriaAppService>();
        services.AddScoped<IRotinasAgendaAppService, RotinasAgendaAppService>();
        services.AddScoped<IDocumentoAppService, DocumentoAppService>();

        // Dados da clínica e parâmetros das rotinas (seção "Clinica")
        services.AddSingleton(configuration.GetSection("Clinica").Get<OpcoesClinica>() ?? new OpcoesClinica());
        services.AddSingleton(TimeProvider.System);

        // Canais de notificação: e-mail (se configurado) e, opcionalmente, simulação em log.
        // Para WhatsApp/SMS, implemente INotificador e registre aqui.
        services.AddSingleton(configuration.GetSection("Notificacoes:Email").Get<OpcoesEmail>() ?? new OpcoesEmail());
        services.AddSingleton<INotificador, EmailNotificador>();
        if (configuration.GetValue("Notificacoes:SimularEnvio", false))
        {
            services.AddSingleton<INotificador, RegistroLogNotificador>();
        }
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

            AplicarAjustesEsquema(dbAppConsultorio);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AVISO] Migrações de banco de dados: {ex.Message}");
        }
    }

    /// <summary>
    /// Ajustes de esquema idempotentes (podem rodar a cada inicialização sem efeito colateral).
    /// Mantém bancos existentes compatíveis com o modelo enquanto o histórico de migrations do EF
    /// não é reconstruído. Cada bloco roda separado para que uma falha não impeça os demais.
    /// </summary>
    public static void AplicarAjustesEsquema(ConsultorioContexto contexto)
    {
        string[] comandos =
        [
            // Fase 1 — exclusão lógica de cadastros
            """
            ALTER TABLE "Paciente" ADD COLUMN IF NOT EXISTS "Ativo" boolean NOT NULL DEFAULT true;
            ALTER TABLE "Paciente" ADD COLUMN IF NOT EXISTS "DtInativacao" timestamp with time zone NULL;
            ALTER TABLE "Medico" ADD COLUMN IF NOT EXISTS "Ativo" boolean NOT NULL DEFAULT true;
            ALTER TABLE "Medico" ADD COLUMN IF NOT EXISTS "DtInativacao" timestamp with time zone NULL;
            ALTER TABLE "Medico" ADD COLUMN IF NOT EXISTS "EmailUsuario" character varying(256) NULL;
            CREATE INDEX IF NOT EXISTS "IX_Medico_EmailUsuario" ON "Medico" ("EmailUsuario");
            """,
            // Fase 1 — ciclo de vida da consulta e cancelamento de exames
            """
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 1;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "DtFinalizacao" timestamp with time zone NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "DtAnulacao" timestamp with time zone NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "MotivoAnulacao" character varying(500) NULL;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "Cancelado" boolean NOT NULL DEFAULT false;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "DtCancelamento" timestamp with time zone NULL;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "MotivoCancelamento" character varying(500) NULL;
            """,
            // Fase 1 — adendos do prontuário
            """
            CREATE TABLE IF NOT EXISTS "ConsultaAdendo" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "IdConsulta" integer NOT NULL REFERENCES "Consulta"("Id") ON DELETE CASCADE,
                "Texto" character varying(2000) NOT NULL,
                "Autor" character varying(256) NOT NULL,
                "DtCriacao" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_ConsultaAdendo_IdConsulta" ON "ConsultaAdendo" ("IdConsulta");
            """,
            // Fase 1 — trilha de auditoria
            """
            CREATE TABLE IF NOT EXISTS "RegistroAuditoria" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "Usuario" character varying(256) NOT NULL,
                "Acao" character varying(30) NOT NULL,
                "Recurso" character varying(60) NOT NULL,
                "IdRecurso" character varying(60) NULL,
                "Metodo" character varying(10) NOT NULL,
                "Caminho" character varying(400) NOT NULL,
                "StatusCode" integer NOT NULL,
                "Ip" character varying(64) NULL,
                "DtCriacao" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_RegistroAuditoria_DtCriacao" ON "RegistroAuditoria" ("DtCriacao");
            CREATE INDEX IF NOT EXISTS "IX_RegistroAuditoria_Recurso_IdRecurso" ON "RegistroAuditoria" ("Recurso", "IdRecurso");
            """,
            // Fase 2 — e-mail de contato para lembretes
            """
            ALTER TABLE "Pessoa" ADD COLUMN IF NOT EXISTS "Email" character varying(256) NULL;
            """,
            // Fase 2 — situação, tipo e duração do agendamento; confirmação pelo paciente
            """
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "Tipo" integer NOT NULL DEFAULT 1;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "DuracaoMinutos" integer NOT NULL DEFAULT 30;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "Status" integer NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "DtConfirmacao" timestamp with time zone NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "DtChegada" timestamp with time zone NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "DtInicioAtendimento" timestamp with time zone NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "DtFimAtendimento" timestamp with time zone NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "TokenConfirmacao" uuid NULL;
            ALTER TABLE "Agenda" ADD COLUMN IF NOT EXISTS "LembreteEnviadoEm" timestamp with time zone NULL;
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_Agenda_TokenConfirmacao" ON "Agenda" ("TokenConfirmacao");
            CREATE INDEX IF NOT EXISTS "IX_Agenda_Data_IdMedico" ON "Agenda" ("Data", "IdMedico");
            -- Horários já ocupados antes desta versão passam a ter situação "Agendado" (ou "Atendido" se já têm consulta)
            UPDATE "Agenda" SET "Status" = CASE WHEN "IdConsulta" IS NOT NULL THEN 5 ELSE 1 END
             WHERE "Status" IS NULL AND ("IdPaciente" IS NOT NULL OR "IdConsulta" IS NOT NULL OR "IdExame" IS NOT NULL);
            UPDATE "Agenda" SET "TokenConfirmacao" = gen_random_uuid()
             WHERE "TokenConfirmacao" IS NULL AND "IdPaciente" IS NOT NULL;
            """,
            // Fase 2 — bloqueios de agenda
            """
            CREATE TABLE IF NOT EXISTS "BloqueioAgenda" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "IdMedico" integer NULL REFERENCES "Medico"("Id") ON DELETE CASCADE,
                "DataInicio" timestamp with time zone NOT NULL,
                "DataFim" timestamp with time zone NOT NULL,
                "Motivo" character varying(200) NOT NULL,
                "DtCriacao" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_BloqueioAgenda_DataInicio_DataFim" ON "BloqueioAgenda" ("DataInicio", "DataFim");
            """,
            // Fase 3 — alertas clínicos do paciente
            """
            ALTER TABLE "Paciente" ADD COLUMN IF NOT EXISTS "Alergias" character varying(2000) NULL;
            ALTER TABLE "Paciente" ADD COLUMN IF NOT EXISTS "MedicamentosEmUso" character varying(2000) NULL;
            ALTER TABLE "Paciente" ADD COLUMN IF NOT EXISTS "CondicoesCronicas" character varying(2000) NULL;
            """,
            // Fase 3 — evolução SOAP, CID e sinais vitais na consulta
            """
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "Subjetivo" character varying(4000) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "Objetivo" character varying(4000) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "Avaliacao" character varying(4000) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "Plano" character varying(4000) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "CidCodigo" character varying(10) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "CidDescricao" character varying(200) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvPressaoSistolica" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvPressaoDiastolica" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvFrequenciaCardiaca" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvFrequenciaRespiratoria" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvTemperatura" numeric(4,1) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvSaturacaoO2" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvPesoKg" numeric(5,2) NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvAlturaCm" integer NULL;
            ALTER TABLE "Consulta" ADD COLUMN IF NOT EXISTS "SvGlicemia" integer NULL;
            """,
            // Fase 3 — ciclo do exame
            """
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "Status" integer NOT NULL DEFAULT 1;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "DtRealizacao" timestamp with time zone NULL;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "DtLaudo" timestamp with time zone NULL;
            ALTER TABLE "Exame" ADD COLUMN IF NOT EXISTS "DtEntrega" timestamp with time zone NULL;
            UPDATE "Exame" SET "Status" = 3, "DtLaudo" = COALESCE("DtLaudo", "DtCriacao")
             WHERE "Status" = 1 AND "Resultado" IS NOT NULL AND btrim("Resultado") <> '';
            """,
            // Fase 3 — documentos clínicos
            """
            CREATE TABLE IF NOT EXISTS "DocumentoClinico" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "Tipo" integer NOT NULL,
                "IdPaciente" integer NOT NULL REFERENCES "Paciente"("Id") ON DELETE CASCADE,
                "IdMedico" integer NOT NULL REFERENCES "Medico"("Id") ON DELETE CASCADE,
                "IdConsulta" integer NULL,
                "Conteudo" character varying(8000) NOT NULL,
                "DiasAfastamento" integer NULL,
                "CidCodigo" character varying(10) NULL,
                "CidDescricao" character varying(200) NULL,
                "CodigoVerificacao" character varying(20) NOT NULL,
                "EmitidoPor" character varying(256) NOT NULL,
                "Cancelado" boolean NOT NULL DEFAULT false,
                "DtCancelamento" timestamp with time zone NULL,
                "MotivoCancelamento" character varying(500) NULL,
                "DtCriacao" timestamp with time zone NOT NULL
            );
            CREATE UNIQUE INDEX IF NOT EXISTS "IX_DocumentoClinico_CodigoVerificacao" ON "DocumentoClinico" ("CodigoVerificacao");
            CREATE INDEX IF NOT EXISTS "IX_DocumentoClinico_IdPaciente" ON "DocumentoClinico" ("IdPaciente");
            """,
            // Fase 3 — anexos de exames
            """
            CREATE TABLE IF NOT EXISTS "ExameAnexo" (
                "Id" integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                "IdExame" integer NOT NULL REFERENCES "Exame"("Id") ON DELETE CASCADE,
                "NomeArquivo" character varying(200) NOT NULL,
                "ContentType" character varying(100) NOT NULL,
                "TamanhoBytes" bigint NOT NULL,
                "Conteudo" bytea NOT NULL,
                "EnviadoPor" character varying(256) NOT NULL,
                "DtCriacao" timestamp with time zone NOT NULL
            );
            CREATE INDEX IF NOT EXISTS "IX_ExameAnexo_IdExame" ON "ExameAnexo" ("IdExame");
            """
        ];

        foreach (var comando in comandos)
        {
            try
            {
                contexto.Database.ExecuteSqlRaw(comando);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AVISO] Ajuste de esquema não aplicado: {ex.Message}");
            }
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
