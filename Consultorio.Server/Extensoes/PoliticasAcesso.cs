using Consultorio.Identity.Modelo.Enumerados;

namespace Consultorio.Server.Extensoes;

/// <summary>
/// Políticas de acesso por perfil.
/// - Clinico: lê e escreve prontuário (consultas, laudos, relatório de prontuário).
/// - Recepcao: toda a equipe — cadastro de pacientes, agenda, solicitação de exames.
/// - Gestao: cadastros administrativos (corpo clínico, inativações).
/// </summary>
public static class PoliticasAcesso
{
    public const string Clinico = nameof(Clinico);
    public const string Recepcao = nameof(Recepcao);
    public const string Gestao = nameof(Gestao);

    public static readonly string[] PerfisClinicos = [nameof(Roles.Administrador), nameof(Roles.Medico)];
    public static readonly string[] PerfisEquipe = [nameof(Roles.Administrador), nameof(Roles.Administrativo), nameof(Roles.Auxiliar), nameof(Roles.Medico)];
    public static readonly string[] PerfisGestao = [nameof(Roles.Administrador), nameof(Roles.Administrativo)];

    public static IServiceCollection AdicionarPoliticasAcesso(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Clinico, p => p.RequireRole(PerfisClinicos));
            options.AddPolicy(Recepcao, p => p.RequireRole(PerfisEquipe));
            options.AddPolicy(Gestao, p => p.RequireRole(PerfisGestao));
        });
        return services;
    }
}
