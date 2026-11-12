using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Rotinas periódicas da clínica (executadas junto com as da agenda):
/// cobranças dos atendimentos, aviso de laudo pronto, lembrete de retorno,
/// resumo do dia e indicadores do mês. As mensagens ao paciente nunca trazem conteúdo clínico.
/// </summary>
public class RotinasClinicaAppService(
    IFinanceiroAppService financeiro,
    IIndicadoresAppService indicadores,
    IExameRepositorio exameRepositorio,
    IConsultaRepositorio consultaRepositorio,
    IAgendaRepositorio agendaRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IExecucaoRotinaRepositorio execucaoRepositorio,
    IEnumerable<INotificador> notificadores,
    OpcoesClinica opcoes,
    TimeProvider relogio,
    IDestinatariosInternos? destinatarios = null) : IRotinasClinicaAppService
{
    public const string RotinaResumoDiario = "resumo-diario";
    public const string RotinaIndicadoresMensais = "indicadores-mensais";

    private DateTime AgoraLocal => TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, opcoes.ObterFuso());

    public Task<int> GerarCobrancasAsync(CancellationToken cancellationToken = default)
    {
        var hoje = AgoraLocal.Date;
        return financeiro.GerarCobrancasDeAtendimentosAsync(hoje.AddDays(-7), hoje, cancellationToken);
    }

    public async Task<ResultadoRotinaResponse> AvisarLaudosProntosAsync(CancellationToken cancellationToken = default)
    {
        var limite = relogio.GetUtcNow().UtcDateTime.AddDays(-30);
        var prontos = (await exameRepositorio.ObterEmAndamento())
            .Where(e => e.Status == StatusExame.Laudado && e.AvisoLaudoEnviadoEm is null && e.DtLaudo >= limite)
            .ToList();

        int enviados = 0, semContato = 0, falhas = 0;
        foreach (var exame in prontos)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var pessoa = exame.Paciente?.Pessoa;
            if (pessoa is null) continue;

            if (string.IsNullOrWhiteSpace(pessoa.Email) && pessoa.Telefone is null)
            {
                // Sem nenhum contato: não há o que tentar de novo depois.
                exame.MarcarAvisoLaudoEnviado();
                await exameRepositorio.Atualizar(exame);
                semContato++;
                continue;
            }

            var nome = Notificacoes.PrimeiroNome(pessoa.Nome);
            var texto = $"Olá, {nome}!\n\nO resultado do seu exame \"{exame.Descricao}\" já está disponível na {opcoes.Nome}." +
                        "\nRetire na recepção ou agende o retorno com seu médico para a avaliação." +
                        (opcoes.Telefone is null ? "" : $"\nDúvidas: {opcoes.Telefone}");
            var html = Notificacoes.Html(opcoes, "Resultado de exame disponível",
            [
                $"Olá, {Notificacoes.H(nome)}!",
                $"O resultado do seu exame <strong>{Notificacoes.H(exame.Descricao)}</strong> já está disponível.",
                "Retire na recepção ou agende o retorno com seu médico para a avaliação."
            ]);
            var mensagem = new MensagemNotificacao(pessoa.Nome, pessoa.Email, pessoa.Telefone?.Valor,
                "Seu resultado de exame está disponível", texto, html);

            switch (await Notificacoes.EnviarAsync(notificadores, mensagem, cancellationToken))
            {
                case true:
                    exame.MarcarAvisoLaudoEnviado();
                    await exameRepositorio.Atualizar(exame);
                    enviados++;
                    break;
                case false: falhas++; break;
                default: semContato++; break;
            }
        }

        return new ResultadoRotinaResponse(prontos.Count, enviados, semContato, falhas);
    }

    public async Task<ResultadoRotinaResponse> LembrarRetornosAsync(CancellationToken cancellationToken = default)
    {
        var hoje = AgoraLocal.Date;
        var consultas = (await consultaRepositorio.ObterComRetornoEntre(hoje, hoje.AddDays(Math.Max(0, opcoes.AntecedenciaLembreteRetornoDias))))
            .Where(c => c.LembreteRetornoEnviadoEm is null)
            .ToList();

        int enviados = 0, semContato = 0, falhas = 0, processados = 0;
        foreach (var consulta in consultas)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var pessoa = consulta.Paciente?.Pessoa;
            if (pessoa is null) continue;

            // Quem já tem horário marcado depois da consulta não precisa do lembrete.
            var diaConsulta = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(consulta.DtCriacao, DateTimeKind.Utc), opcoes.ObterFuso()).Date;
            var agendas = await agendaRepositorio.ObterPorPaciente(consulta.IdPaciente);
            var jaAgendou = agendas.Any(a => a.Data.Date > diaConsulta && a.Status.HasValue && a.Status != StatusAgendamento.Faltou);
            processados++;
            if (jaAgendou || (string.IsNullOrWhiteSpace(pessoa.Email) && pessoa.Telefone is null))
            {
                consulta.MarcarLembreteRetornoEnviado();
                await consultaRepositorio.Atualizar(consulta);
                if (!jaAgendou) semContato++;
                continue;
            }

            var nome = Notificacoes.PrimeiroNome(pessoa.Nome);
            var medico = consulta.Medico?.Pessoa?.Nome;
            var data = consulta.RetornoEm!.Value.ToString("dd/MM/yyyy");
            var texto = $"Olá, {nome}!\n\nSeu retorno na {opcoes.Nome}" + (medico is null ? "" : $" com {medico}") +
                        $" está previsto para {data}, e ainda não encontramos um horário marcado." +
                        (opcoes.Telefone is null ? "\nEntre em contato para agendar." : $"\nAgende pelo telefone {opcoes.Telefone}.");
            var html = Notificacoes.Html(opcoes, "Hora de agendar seu retorno",
            [
                $"Olá, {Notificacoes.H(nome)}!",
                $"Seu retorno{(medico is null ? "" : $" com {Notificacoes.H(medico)}")} está previsto para <strong>{data}</strong>, e ainda não encontramos um horário marcado.",
                opcoes.Telefone is null ? "Entre em contato com a clínica para agendar." : $"Agende pelo telefone <strong>{Notificacoes.H(opcoes.Telefone)}</strong>."
            ]);
            var mensagem = new MensagemNotificacao(pessoa.Nome, pessoa.Email, pessoa.Telefone?.Valor,
                $"Lembrete: agende seu retorno ({data})", texto, html);

            switch (await Notificacoes.EnviarAsync(notificadores, mensagem, cancellationToken))
            {
                case true:
                    consulta.MarcarLembreteRetornoEnviado();
                    await consultaRepositorio.Atualizar(consulta);
                    enviados++;
                    break;
                case false: falhas++; break;
                default: semContato++; break;
            }
        }

        return new ResultadoRotinaResponse(processados, enviados, semContato, falhas);
    }

    public async Task<bool> EnviarResumoDiarioAsync(CancellationToken cancellationToken = default)
    {
        var agora = AgoraLocal;
        if (agora.Hour < Math.Clamp(opcoes.HoraResumoDiario, 0, 23)) return false;
        var referencia = agora.ToString("yyyy-MM-dd");
        if (!await ReservarExecucaoAsync(RotinaResumoDiario, referencia)) return false;

        var hoje = agora.Date;
        var agendaDoDia = await agendaRepositorio.ObterPorPeriodo(hoje, hoje);
        var aguardandoLaudo = (await exameRepositorio.ObterEmAndamento())
            .Where(e => e.Status == StatusExame.Realizado && e.IdMedico.HasValue)
            .GroupBy(e => e.IdMedico!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        // Médicos: só horários e quantidades (sem nomes de pacientes no e-mail).
        foreach (var medico in (await medicoRepositorio.ObterTodosDetalhado()).Where(m => m.Ativo))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var email = medico.EmailUsuario ?? medico.Pessoa?.Email;
            if (string.IsNullOrWhiteSpace(email)) continue;

            var meus = agendaDoDia
                .Where(a => a.IdMedico == medico.Id && a.IdPaciente.HasValue && a.Status.HasValue && a.Status != StatusAgendamento.Faltou)
                .OrderBy(a => a.Hora)
                .ToList();
            var laudos = aguardandoLaudo.GetValueOrDefault(medico.Id);
            if (meus.Count == 0 && laudos == 0) continue;

            var nome = Notificacoes.PrimeiroNome(medico.Pessoa?.Nome?.Replace("Dr. ", "").Replace("Dra. ", ""));
            var agendaTexto = meus.Count == 0
                ? "Nenhum atendimento agendado para hoje."
                : $"{meus.Count} atendimento(s) hoje, das {meus[0].Hora:hh\\:mm} às {meus[^1].Hora:hh\\:mm}.";
            var porTipo = string.Join(", ", meus.GroupBy(a => a.Tipo).Select(g => $"{g.Count()} {g.Key.ObterNome().ToLowerInvariant()}"));
            var laudoTexto = laudos == 0 ? null : $"{laudos} exame(s) realizado(s) aguardando seu laudo.";

            var texto = $"Bom dia, {nome}!\n\n{agendaTexto}" + (porTipo.Length > 0 ? $"\n({porTipo})" : "") +
                        (laudoTexto is null ? "" : $"\n{laudoTexto}") + "\n\nOs detalhes estão na fila do dia do sistema.";
            var paragrafos = new List<string> { $"Bom dia, {Notificacoes.H(nome)}!", $"<strong>{Notificacoes.H(agendaTexto)}</strong>" };
            if (porTipo.Length > 0) paragrafos.Add(Notificacoes.H(porTipo));
            if (laudoTexto is not null) paragrafos.Add(Notificacoes.H(laudoTexto));
            paragrafos.Add("Os detalhes estão na fila do dia do sistema.");

            await Notificacoes.EnviarAsync(notificadores, new MensagemNotificacao(medico.Pessoa?.Nome ?? email, email, null,
                $"Sua agenda de hoje ({hoje:dd/MM})", texto, Notificacoes.Html(opcoes, "Resumo do dia", paragrafos)), cancellationToken);
        }

        // Gestão: quantidade de pendências por tipo.
        if (destinatarios is not null)
        {
            var pendencias = await indicadores.ObterPendenciasAsync();
            if (pendencias.Total > 0)
            {
                var linhas = new List<(string rotulo, int qtd)>
                {
                    ("Exames atrasados", pendencias.ExamesAtrasados.Count),
                    ("Laudos prontos não entregues", pendencias.LaudosNaoEntregues.Count),
                    ("Carteirinhas vencidas ou vencendo", pendencias.Carteirinhas.Count),
                    ("Cobranças vencidas", pendencias.CobrancasVencidas.Count),
                    ("Consultas abertas há mais de 24 h", pendencias.ConsultasAbertas.Count),
                    ("Retornos sem horário marcado", pendencias.RetornosSemAgendamento.Count)
                }.Where(l => l.qtd > 0).ToList();

                var texto = $"Pendências da {opcoes.Nome} em {hoje:dd/MM/yyyy}:\n" +
                            string.Join("\n", linhas.Select(l => $"- {l.rotulo}: {l.qtd}")) +
                            "\n\nVeja a lista completa em Pendências, no sistema.";
                var html = Notificacoes.Html(opcoes, $"Pendências de {hoje:dd/MM/yyyy}",
                    linhas.Select(l => $"{Notificacoes.H(l.rotulo)}: <strong>{l.qtd}</strong>")
                        .Append("Veja a lista completa em <strong>Pendências</strong>, no sistema."));
                await EnviarParaGestaoAsync($"Pendências do dia ({hoje:dd/MM})", texto, html, cancellationToken);
            }
        }

        return true;
    }

    public async Task<bool> EnviarIndicadoresMensaisAsync(CancellationToken cancellationToken = default)
    {
        if (destinatarios is null) return false;
        var agora = AgoraLocal;
        if (agora.Hour < Math.Clamp(opcoes.HoraResumoDiario, 0, 23)) return false;

        var inicioMesAtual = new DateTime(agora.Year, agora.Month, 1);
        var inicio = inicioMesAtual.AddMonths(-1);
        var fim = inicioMesAtual.AddDays(-1);
        if (!await ReservarExecucaoAsync(RotinaIndicadoresMensais, inicio.ToString("yyyy-MM"))) return false;

        var i = await indicadores.ObterAsync(inicio, fim);
        var mes = inicio.ToString("MMMM 'de' yyyy", Notificacoes.PtBr);
        var moeda = (decimal v) => v.ToString("C", Notificacoes.PtBr);
        var linhas = new List<string>
        {
            $"Atendimentos realizados: {i.Atendidos} de {i.Agendamentos} agendados",
            $"Faltas: {i.Faltas} ({i.TaxaFaltas:0.#}%) · Confirmação: {i.TaxaConfirmacao:0.#}%",
            $"Ocupação da agenda: {i.TaxaOcupacao:0.#}%",
            $"Pacientes novos: {i.NovosPacientes}",
            $"Exames solicitados: {i.ExamesSolicitados} · laudados: {i.ExamesLaudados}" +
                (i.TempoMedioLaudoDias is null ? "" : $" · tempo médio de laudo: {i.TempoMedioLaudoDias:0.#} dia(s)"),
            $"Recebido: {moeda(i.Financeiro.Recebido)} em {i.Financeiro.QuantidadeRecebida} pagamento(s) · ticket médio {moeda(i.Financeiro.TicketMedio)}",
            $"A receber: {moeda(i.Financeiro.AReceber)} (vencido: {moeda(i.Financeiro.Vencido)})"
        };

        var texto = $"Indicadores de {mes} — {opcoes.Nome}\n\n" + string.Join("\n", linhas.Select(l => $"- {l}")) +
                    "\n\nO painel completo está em Indicadores, no sistema.";
        var html = Notificacoes.Html(opcoes, $"Indicadores de {mes}",
            linhas.Select(Notificacoes.H).Append("O painel completo está em <strong>Indicadores</strong>, no sistema."));
        await EnviarParaGestaoAsync($"Indicadores de {mes}", texto, html, cancellationToken);
        return true;
    }

    /// <summary>Registra a execução do período; false se ela já tinha sido feita (outro ciclo ou outra instância).</summary>
    private async Task<bool> ReservarExecucaoAsync(string nome, string referencia)
    {
        var execucao = await execucaoRepositorio.ObterPorNome(nome);
        if (execucao is null)
        {
            await execucaoRepositorio.Adicionar(new ExecucaoRotina(nome, referencia));
            return true;
        }
        if (execucao.JaExecutou(referencia)) return false;
        execucao.Registrar(referencia);
        await execucaoRepositorio.Atualizar(execucao);
        return true;
    }

    private async Task EnviarParaGestaoAsync(string assunto, string texto, string html, CancellationToken cancellationToken)
    {
        if (destinatarios is null) return;
        var emails = await destinatarios.ObterEmailsAsync("Administrador", "Administrativo");
        foreach (var email in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await Notificacoes.EnviarAsync(notificadores, new MensagemNotificacao(email, email, null, assunto, texto, html), cancellationToken);
        }
    }
}
