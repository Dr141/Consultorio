using System.Net;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Rotinas automáticas da agenda, executadas periodicamente pelo servidor:
/// lembrete com link de confirmação e marcação de faltas.
/// O relógio (TimeProvider) é injetado para que as regras de horário possam ser testadas.
/// </summary>
public class RotinasAgendaAppService(
    IAgendaRepositorio agendaRepositorio,
    IEnumerable<INotificador> notificadores,
    OpcoesClinica opcoes,
    TimeProvider relogio) : IRotinasAgendaAppService
{
    public async Task<ResultadoRotinaResponse> EnviarLembretesAsync(CancellationToken cancellationToken = default)
    {
        var fuso = opcoes.ObterFuso();
        var agoraUtc = relogio.GetUtcNow().UtcDateTime;
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(agoraUtc, fuso);
        var limiteLocal = agoraLocal.AddHours(opcoes.AntecedenciaLembreteHoras);

        var candidatos = (await agendaRepositorio.ObterPorPeriodo(agoraLocal.Date, limiteLocal.Date))
            .Where(a => a.Status == StatusAgendamento.Agendado
                        && a.LembreteEnviadoEm is null
                        && a.InicioLocal > agoraLocal
                        && a.InicioLocal <= limiteLocal)
            .ToList();

        int enviados = 0, semContato = 0, falhas = 0;
        foreach (var agenda in candidatos)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var resultado = await TentarEnviarAsync(agenda, cancellationToken);
            switch (resultado)
            {
                case true: enviados++; break;
                case false: falhas++; break;
                default: semContato++; break;
            }
        }

        return new ResultadoRotinaResponse(candidatos.Count, enviados, semContato, falhas);
    }

    public async Task<int> MarcarFaltasAsync(CancellationToken cancellationToken = default)
    {
        var fuso = opcoes.ObterFuso();
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, fuso);
        var tolerancia = TimeSpan.FromMinutes(Math.Max(0, opcoes.ToleranciaFaltaMinutos));

        // Olha os últimos dias para recuperar execuções perdidas (servidor desligado, por exemplo).
        var recentes = await agendaRepositorio.ObterPorPeriodo(agoraLocal.Date.AddDays(-7), agoraLocal.Date);
        var faltas = recentes
            .Where(a => a.Status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado
                        && a.InicioLocal + tolerancia < agoraLocal)
            .ToList();

        foreach (var agenda in faltas)
        {
            if (cancellationToken.IsCancellationRequested) break;
            agenda.RegistrarFalta();
            await agendaRepositorio.Atualizar(agenda);
        }

        return faltas.Count;
    }

    public async Task<bool> EnviarLembreteAsync(int idAgenda, CancellationToken cancellationToken = default)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(idAgenda)
            ?? throw new DomainException($"Horário de agenda com id {idAgenda} não encontrado.");
        DomainException.When(agenda.Status is not (StatusAgendamento.Agendado or StatusAgendamento.Confirmado),
            "Só é possível enviar lembrete de agendamentos ainda não atendidos.");

        var resultado = await TentarEnviarAsync(agenda, cancellationToken);
        DomainException.When(resultado is null,
            "Nenhum canal disponível para este paciente. Cadastre o e-mail do paciente e configure o envio de e-mails.");
        DomainException.When(resultado == false, "O envio do lembrete falhou. Tente novamente em instantes.");
        return true;
    }

    /// <summary>true = enviado; false = falhou; null = paciente sem contato ou nenhum canal configurado.</summary>
    private async Task<bool?> TentarEnviarAsync(Agenda agenda, CancellationToken cancellationToken)
    {
        var mensagem = MontarMensagem(agenda);
        if (mensagem is null) return null;

        var canal = notificadores.FirstOrDefault(n => n.PodeEnviar(mensagem));
        if (canal is null) return null;

        bool enviado;
        try
        {
            enviado = await canal.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception)
        {
            enviado = false;
        }

        if (enviado)
        {
            agenda.MarcarLembreteEnviado();
            await agendaRepositorio.Atualizar(agenda);
        }

        return enviado;
    }

    public MensagemNotificacao? MontarMensagem(Agenda agenda)
    {
        var pessoa = agenda.Paciente?.Pessoa;
        if (pessoa is null || agenda.TokenConfirmacao is null) return null;

        var nome = pessoa.Nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? pessoa.Nome;
        var quando = agenda.InicioLocal.ToString("dddd, dd/MM 'às' HH:mm", new System.Globalization.CultureInfo("pt-BR"));
        var medico = agenda.Medico?.Pessoa?.Nome;
        var baseUrl = opcoes.UrlPublica.TrimEnd('/');
        var linkConfirmar = $"{baseUrl}/api/publico/confirmacao/{agenda.TokenConfirmacao}?resposta=sim";
        var linkDesmarcar = $"{baseUrl}/api/publico/confirmacao/{agenda.TokenConfirmacao}?resposta=nao";
        var tipo = agenda.Tipo.ObterNome().ToLowerInvariant();

        var texto =
            $"Olá, {nome}!\n\n" +
            $"Lembramos do seu atendimento ({tipo}) na {opcoes.Nome}: {quando}" +
            (medico is null ? "." : $", com {medico}.") + "\n\n" +
            $"Confirmar presença: {linkConfirmar}\n" +
            $"Não poderá comparecer? Desmarque: {linkDesmarcar}\n\n" +
            (opcoes.Endereco is null ? "" : $"Endereço: {opcoes.Endereco}\n") +
            (opcoes.Telefone is null ? "" : $"Dúvidas: {opcoes.Telefone}\n");

        string H(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:520px;margin:auto;color:#0f172a">
              <h2 style="color:#1976d2;margin-bottom:4px">{H(opcoes.Nome)}</h2>
              <p>Olá, {H(nome)}!</p>
              <p>Lembramos do seu atendimento (<strong>{H(tipo)}</strong>):</p>
              <p style="font-size:18px;margin:8px 0"><strong>{H(quando)}</strong>{(medico is null ? "" : $"<br/>com {H(medico)}")}</p>
              <p style="margin:24px 0">
                <a href="{linkConfirmar}" style="background:#16a34a;color:#fff;padding:12px 20px;border-radius:8px;text-decoration:none;font-weight:bold">Confirmar presença</a>
                &nbsp;
                <a href="{linkDesmarcar}" style="color:#dc2626;padding:12px 8px;text-decoration:underline">Não poderei ir</a>
              </p>
              {(opcoes.Endereco is null ? "" : $"<p>Endereço: {H(opcoes.Endereco)}</p>")}
              {(opcoes.Telefone is null ? "" : $"<p>Dúvidas: {H(opcoes.Telefone)}</p>")}
              <p style="color:#64748b;font-size:12px">Mensagem automática. Por favor, não responda este e-mail.</p>
            </div>
            """;

        return new MensagemNotificacao(
            pessoa.Nome,
            pessoa.Email,
            pessoa.Telefone?.Valor,
            $"Lembrete: seu atendimento em {agenda.InicioLocal:dd/MM 'às' HH:mm}",
            texto,
            html);
    }
}
