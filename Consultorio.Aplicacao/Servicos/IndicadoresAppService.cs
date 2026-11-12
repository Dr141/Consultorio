using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>Indicadores de gestão do período e lista de pendências operacionais do momento.</summary>
public class IndicadoresAppService(
    IAgendaRepositorio agendaRepositorio,
    IConsultaRepositorio consultaRepositorio,
    IExameRepositorio exameRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    ICobrancaRepositorio cobrancaRepositorio,
    OpcoesClinica opcoes,
    TimeProvider relogio) : IIndicadoresAppService
{
    private DateTime AgoraLocal => TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, opcoes.ObterFuso());

    private DateTime DiaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), opcoes.ObterFuso()).Date;

    private static decimal Percentual(int parte, int total) => total == 0 ? 0 : Math.Round(100m * parte / total, 1);

    public async Task<IndicadoresResponse> ObterAsync(DateTime inicio, DateTime fim, int? idMedico = null)
    {
        var ini = inicio.Date;
        var f = fim.Date;
        DomainException.When(ini > f, "A data inicial não pode ser posterior à final.");
        DomainException.When((f - ini).TotalDays > 366, "Consulte no máximo um ano por vez.");
        var noPeriodo = (DateTime utc) => DiaLocal(utc) >= ini && DiaLocal(utc) <= f;

        // Agenda
        var horarios = await agendaRepositorio.ObterPorPeriodo(ini, f, idMedico);
        var agendados = horarios.Where(a => a.IdPaciente.HasValue && a.Status.HasValue).ToList();
        var atendidos = agendados.Count(a => a.Status == StatusAgendamento.Atendido);
        var faltas = agendados.Count(a => a.Status == StatusAgendamento.Faltou);
        var confirmados = agendados.Count(a => a.DtConfirmacao.HasValue);
        var livres = horarios.Count(a => !a.IdPaciente.HasValue);

        var porMedico = horarios
            .Where(a => a.IdMedico.HasValue)
            .GroupBy(a => a.IdMedico!.Value)
            .Select(g => new IndicadorMedico(
                g.Key,
                g.First().Medico?.Pessoa?.Nome ?? $"Médico #{g.Key}",
                g.Count(a => a.IdPaciente.HasValue && a.Status.HasValue),
                g.Count(a => a.Status == StatusAgendamento.Atendido),
                g.Count(a => a.Status == StatusAgendamento.Faltou)))
            .OrderByDescending(m => m.Atendidos)
            .ToList();

        var porTipo = agendados
            .GroupBy(a => a.Tipo)
            .Select(g => new ItemIndicador(g.Key.ObterNome(), g.Count()))
            .OrderByDescending(i => i.Quantidade)
            .ToList();

        // Consultas e diagnósticos
        var consultas = (await consultaRepositorio.ObterPorPeriodo(ini, f))
            .Where(c => noPeriodo(c.DtCriacao) && c.Status != StatusConsulta.Anulada)
            .Where(c => idMedico is null || c.IdMedico == idMedico)
            .ToList();
        var diagnosticos = consultas
            .Where(c => !string.IsNullOrWhiteSpace(c.CidCodigo))
            .GroupBy(c => c.CidCodigo!)
            .Select(g => new ItemIndicador($"{g.Key} · {g.First().CidDescricao}".TrimEnd(' ', '·'), g.Count()))
            .OrderByDescending(i => i.Quantidade)
            .Take(10)
            .ToList();

        // Exames
        var exames = (await exameRepositorio.ObterTodosDetalhado())
            .Where(e => !e.Cancelado && noPeriodo(e.DtCriacao))
            .Where(e => idMedico is null || e.IdMedico == idMedico)
            .ToList();
        var laudados = exames.Where(e => e.DtLaudo.HasValue).ToList();
        decimal? tempoMedioLaudo = laudados.Count == 0
            ? null
            : Math.Round((decimal)laudados.Average(e => (e.DtLaudo!.Value - e.DtCriacao).TotalDays), 1);
        var atrasados = (await exameRepositorio.ObterEmAndamento())
            .Count(e => EstaAtrasado(e) && (idMedico is null || e.IdMedico == idMedico));

        // Pacientes novos
        var novosPacientes = (await pacienteRepositorio.ObterTodosDetalhado()).Count(p => noPeriodo(p.DtCriacao));

        // Financeiro (não é separado por médico)
        var fuso = opcoes.ObterFuso();
        var iniUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(ini, DateTimeKind.Unspecified), fuso);
        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(f.AddDays(1), DateTimeKind.Unspecified), fuso);
        var financeiro = FinanceiroAppService.MontarResumo(
            await cobrancaRepositorio.ObterPagasNoPeriodo(iniUtc, fimUtc),
            await cobrancaRepositorio.ObterPendentes(),
            AgoraLocal.Date);

        return new IndicadoresResponse(
            ini, f,
            agendados.Count,
            atendidos,
            faltas,
            Percentual(faltas, atendidos + faltas),
            Percentual(confirmados, agendados.Count),
            livres,
            Percentual(horarios.Count - livres, horarios.Count),
            novosPacientes,
            consultas.Count,
            exames.Count,
            laudados.Count,
            atrasados,
            tempoMedioLaudo,
            financeiro,
            porMedico,
            porTipo,
            diagnosticos);
    }

    private bool EstaAtrasado(Exame e) =>
        e.Status is StatusExame.Solicitado or StatusExame.Realizado
        && DiaLocal(e.DtCriacao) < AgoraLocal.Date.AddDays(-Math.Max(1, opcoes.PrazoExameAtrasadoDias));

    public async Task<PendenciasResponse> ObterPendenciasAsync()
    {
        var agora = AgoraLocal;
        var hoje = agora.Date;
        static string Nome(Paciente? p) => p?.Pessoa?.Nome ?? "Paciente";

        var emAndamento = await exameRepositorio.ObterEmAndamento();
        var examesAtrasados = emAndamento
            .Where(EstaAtrasado)
            .Select(e => new PendenciaItem("ExameAtrasado", e.Descricao,
                $"{Nome(e.Paciente)} · {(e.Status == StatusExame.Realizado ? "realizado, aguardando laudo" : "solicitado, ainda não realizado")}",
                e.DtCriacao, e.IdPaciente, e.Id))
            .ToList();

        var limiteEntrega = hoje.AddDays(-Math.Max(1, opcoes.PrazoEntregaLaudoDias));
        var laudosNaoEntregues = emAndamento
            .Where(e => e.Status == StatusExame.Laudado && e.DtLaudo.HasValue && DiaLocal(e.DtLaudo.Value) < limiteEntrega)
            .Select(e => new PendenciaItem("LaudoNaoEntregue", e.Descricao, $"{Nome(e.Paciente)} · laudo pronto, ainda não entregue",
                e.DtLaudo, e.IdPaciente, e.Id))
            .ToList();

        var pacientes = (await pacienteRepositorio.ObterTodosDetalhado()).Where(p => p.Ativo).ToList();
        var carteirinhas = pacientes
            .Where(p => p.CarteirinhaVencida(hoje) || p.CarteirinhaVencendo(hoje, opcoes.AvisoValidadeCarteirinhaDias))
            .OrderBy(p => p.ValidadeCarteirinha)
            .Select(p => new PendenciaItem("Carteirinha", Nome(p),
                $"{p.Convenio?.Nome ?? "Convênio"} · carteirinha {(p.CarteirinhaVencida(hoje) ? "vencida" : "vence")} em {p.ValidadeCarteirinha:dd/MM/yyyy}",
                p.ValidadeCarteirinha, p.Id, p.Id))
            .ToList();

        var cobrancasVencidas = (await cobrancaRepositorio.ObterPendentes())
            .Where(c => c.Vencida(hoje))
            .Select(c => new PendenciaItem("CobrancaVencida", Nome(c.Paciente),
                $"{c.Descricao} · {c.ValorFinal.ToString("C", Notificacoes.PtBr)} · venceu em {c.DtVencimento:dd/MM/yyyy}",
                c.DtVencimento, c.IdPaciente, c.Id))
            .ToList();

        var recentes = await consultaRepositorio.ObterPorPeriodo(hoje.AddDays(-30), hoje);
        var limiteAberta = relogio.GetUtcNow().UtcDateTime.AddHours(-24);
        var consultasAbertas = recentes
            .Where(c => c.Status == StatusConsulta.Aberta && c.DtCriacao < limiteAberta)
            .OrderBy(c => c.DtCriacao)
            .Select(c => new PendenciaItem("ConsultaAberta", c.Medico?.Pessoa?.Nome ?? "Médico",
                "Consulta registrada e ainda não finalizada", c.DtCriacao, c.IdPaciente, c.Id))
            .ToList();

        var retornosSemAgendamento = await RetornosSemAgendamentoAsync(hoje);

        return new PendenciasResponse(examesAtrasados, laudosNaoEntregues, carteirinhas, cobrancasVencidas, consultasAbertas, retornosSemAgendamento);
    }

    /// <summary>Retornos previstos (até 7 dias à frente, ou já vencidos há até 30 dias) sem horário marcado depois da consulta.</summary>
    private async Task<List<PendenciaItem>> RetornosSemAgendamentoAsync(DateTime hoje)
    {
        var consultas = await consultaRepositorio.ObterComRetornoEntre(hoje.AddDays(-30), hoje.AddDays(7));
        if (consultas.Count == 0) return [];

        var desde = consultas.Min(c => DiaLocal(c.DtCriacao)).AddDays(1);
        var agendas = await agendaRepositorio.ObterPorPeriodo(desde, hoje.AddDays(180));
        var resultado = new List<PendenciaItem>();
        foreach (var c in consultas.OrderBy(c => c.RetornoEm))
        {
            var diaConsulta = DiaLocal(c.DtCriacao);
            var temHorario = agendas.Any(a => a.IdPaciente == c.IdPaciente && a.Data.Date > diaConsulta
                                              && a.Status.HasValue && a.Status != StatusAgendamento.Faltou);
            if (temHorario) continue;
            resultado.Add(new PendenciaItem("RetornoSemAgendamento", c.Paciente?.Pessoa?.Nome ?? "Paciente",
                $"Retorno previsto para {c.RetornoEm:dd/MM/yyyy}" + (c.Medico?.Pessoa?.Nome is { } m ? $" com {m}" : ""),
                c.RetornoEm, c.IdPaciente, c.Id));
        }
        return resultado;
    }
}
