namespace Consultorio.Aplicacao.Configuracao;

/// <summary>
/// Dados da clínica e parâmetros das rotinas automáticas (seção "Clinica" do appsettings).
/// </summary>
public class OpcoesClinica
{
    public string Nome { get; set; } = "Consultório";
    public string? Endereco { get; set; }
    public string? Telefone { get; set; }

    /// <summary>Fuso usado para "hoje", lembretes e faltas (IANA, ex.: America/Sao_Paulo).</summary>
    public string FusoHorario { get; set; } = "America/Sao_Paulo";

    /// <summary>Endereço público do sistema, usado nos links de confirmação enviados ao paciente.</summary>
    public string UrlPublica { get; set; } = "http://localhost:8080";

    /// <summary>Com quantas horas de antecedência o lembrete é enviado.</summary>
    public int AntecedenciaLembreteHoras { get; set; } = 24;

    /// <summary>Minutos após o horário marcado para considerar falta quem não fez check-in.</summary>
    public int ToleranciaFaltaMinutos { get; set; } = 180;

    public TimeZoneInfo ObterFuso()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(FusoHorario);
        }
        catch (Exception)
        {
            // Sem a base de fusos do sistema, usa o horário de Brasília (UTC-3, sem horário de verão).
            return TimeZoneInfo.CreateCustomTimeZone("Brasilia", TimeSpan.FromHours(-3), "Brasília", "Brasília");
        }
    }
}
