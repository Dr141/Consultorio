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

    // ---- Fase 4: financeiro e rotinas ----

    /// <summary>Gera a cobrança automaticamente quando o atendimento é concluído (se houver preço na tabela).</summary>
    public bool GerarCobrancaAutomatica { get; set; } = true;

    /// <summary>Prazo, em dias, para o convênio pagar (vencimento das cobranças de convênio).</summary>
    public int PrazoPagamentoConvenioDias { get; set; } = 30;

    /// <summary>Exame solicitado/realizado sem laudo há mais dias que isso aparece como atrasado.</summary>
    public int PrazoExameAtrasadoDias { get; set; } = 7;

    /// <summary>Laudo pronto e não entregue há mais dias que isso vira pendência da recepção.</summary>
    public int PrazoEntregaLaudoDias { get; set; } = 15;

    /// <summary>Quantos dias antes da data de retorno o paciente é lembrado de agendar.</summary>
    public int AntecedenciaLembreteRetornoDias { get; set; } = 5;

    /// <summary>Carteirinhas que vencem dentro deste prazo aparecem nas pendências.</summary>
    public int AvisoValidadeCarteirinhaDias { get; set; } = 30;

    /// <summary>Hora local a partir da qual o resumo do dia é enviado aos médicos e à gestão.</summary>
    public int HoraResumoDiario { get; set; } = 7;

    /// <summary>Usuários sem atividade há mais dias que isso são destacados na revisão de acessos.</summary>
    public int DiasInatividadeRevisaoAcessos { get; set; } = 90;

    // ---- Fase 5: portal do paciente ----

    /// <summary>Liga/desliga o portal do paciente.</summary>
    public bool PortalHabilitado { get; set; } = true;

    /// <summary>Duração da sessão do paciente no portal, em minutos.</summary>
    public int PortalSessaoMinutos { get; set; } = 30;

    /// <summary>Quantos dias à frente o paciente enxerga horários livres para agendar.</summary>
    public int PortalDiasAgendamento { get; set; } = 30;

    /// <summary>Antecedência mínima, em horas, para agendar pelo portal.</summary>
    public int PortalAntecedenciaAgendamentoHoras { get; set; } = 2;

    /// <summary>Até quantas horas antes o paciente pode desmarcar pelo portal.</summary>
    public int PortalAntecedenciaCancelamentoHoras { get; set; } = 12;

    /// <summary>Máximo de agendamentos futuros simultâneos feitos/visíveis por paciente no portal.</summary>
    public int PortalMaximoAgendamentosFuturos { get; set; } = 2;

    /// <summary>Mostra o texto do laudo e os arquivos do exame no portal quando o laudo estiver pronto.</summary>
    public bool PortalLiberarResultados { get; set; } = true;

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
