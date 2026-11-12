using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Portal;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Portal do paciente. Regras de privacidade:
/// - o acesso é por código enviado ao e-mail já cadastrado na clínica (não há senha a vazar);
/// - a resposta à solicitação é sempre a mesma, exista ou não o cadastro (não revela quem é paciente);
/// - cada consulta é filtrada pelo paciente da sessão: o id vem da sessão, nunca do cliente;
/// - o paciente vê agenda, resultados liberados, documentos e cobranças; nunca a evolução clínica.
/// </summary>
public class PortalPacienteAppService(
    IPessoaRepositorio pessoaRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IAcessoPortalRepositorio acessoRepositorio,
    IAgendaRepositorio agendaRepositorio,
    IAgendaAppService agendaAppService,
    IBloqueioAgendaRepositorio bloqueioRepositorio,
    IExameRepositorio exameRepositorio,
    IExameAnexoRepositorio anexoRepositorio,
    IDocumentoClinicoRepositorio documentoRepositorio,
    ICobrancaRepositorio cobrancaRepositorio,
    IEnumerable<INotificador> notificadores,
    OpcoesClinica opcoes,
    TimeProvider relogio) : IPortalPacienteAppService
{
    public const string MensagemSolicitacao =
        "Se os dados conferirem com um cadastro que tenha e-mail, enviamos um código de acesso. Ele vale por 10 minutos.";
    private const int MaximoSolicitacoesPorHora = 5;
    private static readonly TimeSpan ValidadeCodigo = TimeSpan.FromMinutes(10);

    private DateTime AgoraUtc => relogio.GetUtcNow().UtcDateTime;
    private DateTime AgoraLocal => TimeZoneInfo.ConvertTimeFromUtc(AgoraUtc, opcoes.ObterFuso());

    // ------------------------- Acesso -------------------------

    public async Task<PortalSolicitacaoResponse> SolicitarCodigoAsync(PortalSolicitarCodigoRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        GarantirHabilitado();
        var cpf = new Cpf(request.Cpf);
        // Chave aleatória devolvida mesmo quando não há cadastro: a resposta não indica se o CPF existe.
        var resposta = new PortalSolicitacaoResponse(Guid.NewGuid(), MensagemSolicitacao);

        var pessoa = await pessoaRepositorio.ObterPorCpf(cpf.Valor);
        if (pessoa is null || pessoa.DtNasc.Date != request.DtNasc.Date || string.IsNullOrWhiteSpace(pessoa.Email)) return resposta;

        var paciente = await pacienteRepositorio.ObterPorIdPessoa(pessoa.Id);
        if (paciente is null || !paciente.Ativo) return resposta;

        if (await acessoRepositorio.ContarSolicitacoes(paciente.Id, AgoraUtc.AddHours(-1)) >= MaximoSolicitacoesPorHora) return resposta;

        var (acesso, codigo) = AcessoPortal.Solicitar(paciente.Id, AgoraUtc, ValidadeCodigo, ip);
        await acessoRepositorio.Adicionar(acesso);

        var nome = Notificacoes.PrimeiroNome(pessoa.Nome);
        var texto = $"Olá, {nome}!\n\nSeu código de acesso ao portal da {opcoes.Nome} é: {codigo}\n" +
                    "Ele vale por 10 minutos. Se não foi você quem pediu, ignore esta mensagem.";
        var html = Notificacoes.Html(opcoes, "Código de acesso ao portal",
        [
            $"Olá, {Notificacoes.H(nome)}!",
            "Use o código abaixo para entrar no portal do paciente:",
            $"<span style=\"font-size:28px;font-weight:bold;letter-spacing:6px\">{codigo}</span>",
            "Ele vale por 10 minutos. Se não foi você quem pediu, ignore esta mensagem."
        ]);
        await Notificacoes.EnviarAsync(notificadores,
            new MensagemNotificacao(pessoa.Nome, pessoa.Email, null, $"Código de acesso — {opcoes.Nome}", texto, html), cancellationToken);

        return resposta with { Chave = acesso.Chave };
    }

    public async Task<PortalSessaoResponse> ValidarCodigoAsync(PortalValidarCodigoRequest request)
    {
        GarantirHabilitado();
        const string invalido = "Código inválido ou expirado. Confira o código ou peça um novo.";
        var acesso = await acessoRepositorio.ObterPorChave(request.Chave)
            ?? throw new DomainException(invalido);

        if (!acesso.ValidarCodigo(request.Codigo, AgoraUtc))
        {
            await acessoRepositorio.Atualizar(acesso);
            throw new DomainException(acesso.TentativasEsgotadas
                ? "Muitas tentativas incorretas. Peça um novo código."
                : invalido);
        }

        var token = acesso.AbrirSessao(AgoraUtc, TimeSpan.FromMinutes(Math.Clamp(opcoes.PortalSessaoMinutos, 5, 240)));
        await acessoRepositorio.Atualizar(acesso);

        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(acesso.IdPaciente);
        return new PortalSessaoResponse(token, acesso.TokenExpiraEm!.Value, Notificacoes.PrimeiroNome(paciente?.Pessoa?.Nome));
    }

    public async Task<int?> ValidarSessaoAsync(string? token)
    {
        if (!opcoes.PortalHabilitado || string.IsNullOrWhiteSpace(token) || token.Length > 100) return null;
        var acesso = await acessoRepositorio.ObterPorTokenHash(AcessoPortal.HashToken(token.Trim()));
        if (acesso is null || !acesso.SessaoValida(AgoraUtc)) return null;
        if (acesso.Paciente is { Ativo: false }) return null;
        return acesso.IdPaciente;
    }

    public async Task EncerrarSessaoAsync(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return;
        var acesso = await acessoRepositorio.ObterPorTokenHash(AcessoPortal.HashToken(token.Trim()));
        if (acesso is null) return;
        acesso.Encerrar(AgoraUtc);
        await acessoRepositorio.Atualizar(acesso);
    }

    private void GarantirHabilitado() =>
        DomainException.When(!opcoes.PortalHabilitado, "O portal do paciente não está disponível no momento.");

    // ------------------------- Agenda -------------------------

    public async Task<PortalResumoResponse> ObterResumoAsync(int idPaciente)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(idPaciente)
            ?? throw new DomainException("Paciente não encontrado.");
        var agora = AgoraLocal;
        var agendas = await agendaRepositorio.ObterPorPaciente(idPaciente);

        var futuros = agendas
            .Where(a => a.InicioLocal >= agora.AddHours(-1) && a.Status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado
                        or StatusAgendamento.Aguardando or StatusAgendamento.EmAtendimento)
            .OrderBy(a => a.InicioLocal)
            .Select(a => Mapear(a, agora))
            .ToList();
        var historico = agendas
            .Where(a => a.Status is StatusAgendamento.Atendido or StatusAgendamento.Faltou
                        || (a.Status.HasValue && a.InicioLocal < agora.AddHours(-1)))
            .OrderByDescending(a => a.InicioLocal)
            .Take(20)
            .Select(a => Mapear(a, agora))
            .ToList();

        var maximo = Math.Max(1, opcoes.PortalMaximoAgendamentosFuturos);
        return new PortalResumoResponse(
            Notificacoes.PrimeiroNome(paciente.Pessoa?.Nome),
            paciente.Convenio?.Nome,
            paciente.CarteirinhaVencida(agora.Date),
            futuros,
            historico,
            paciente.Ativo && futuros.Count(f => f.Status is "Agendado" or "Confirmado") < maximo,
            maximo,
            Math.Max(0, opcoes.PortalAntecedenciaCancelamentoHoras),
            opcoes.Nome,
            opcoes.Telefone,
            opcoes.Endereco);
    }

    public async Task<List<PortalMedicoHorariosResponse>> ObterHorariosDisponiveisAsync(int idPaciente)
    {
        var agora = AgoraLocal;
        var inicio = agora.Date;
        var fim = inicio.AddDays(Math.Clamp(opcoes.PortalDiasAgendamento, 1, 120));
        var minimo = agora.AddHours(Math.Max(0, opcoes.PortalAntecedenciaAgendamentoHoras));

        var horarios = await agendaRepositorio.ObterPorPeriodo(inicio, fim);
        var bloqueios = await bloqueioRepositorio.ObterNoPeriodo(inicio, fim);

        return horarios
            .Where(a => HorarioOferecido(a, minimo) && !bloqueios.Any(b => b.Abrange(a.Data, a.IdMedico)))
            .GroupBy(a => a.IdMedico!.Value)
            .Select(g =>
            {
                var medico = g.First().Medico!;
                return new PortalMedicoHorariosResponse(
                    g.Key,
                    medico.Pessoa?.Nome ?? "Médico",
                    medico.Especialidades.Select(e => e.ObterNome()).ToList(),
                    g.OrderBy(a => a.InicioLocal)
                        .Select(a => new PortalHorarioResponse(a.Id, a.InicioLocal, a.DuracaoMinutos, a.Tipo.ObterNome()))
                        .ToList());
            })
            .OrderBy(m => m.Nome, StringComparer.Create(Notificacoes.PtBr, true))
            .ToList();
    }

    /// <summary>Horário livre, de consulta ou retorno, com médico ativo e dentro da antecedência mínima.</summary>
    private static bool HorarioOferecido(Agenda a, DateTime minimoLocal) =>
        a.Disponivel && a.Status is null && a.IdMedico.HasValue && a.Medico is { Ativo: true }
        && a.Tipo is TipoAtendimento.Consulta or TipoAtendimento.Retorno
        && a.InicioLocal >= minimoLocal;

    public async Task<PortalAgendamentoResponse> AgendarAsync(int idPaciente, int idAgenda)
    {
        var paciente = await pacienteRepositorio.ObterId(idPaciente) ?? throw new DomainException("Paciente não encontrado.");
        DomainException.When(!paciente.Ativo, "Cadastro inativo. Entre em contato com a clínica.");

        var agora = AgoraLocal;
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(idAgenda)
            ?? throw new DomainException("Horário não encontrado.");
        var limite = agora.Date.AddDays(Math.Clamp(opcoes.PortalDiasAgendamento, 1, 120) + 1);
        DomainException.When(!HorarioOferecido(agenda, agora.AddHours(Math.Max(0, opcoes.PortalAntecedenciaAgendamentoHoras)))
                             || agenda.InicioLocal >= limite,
            "Este horário não está mais disponível. Escolha outro.");

        var ativos = (await agendaRepositorio.ObterPorPaciente(idPaciente))
            .Count(a => a.InicioLocal >= agora && a.Status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado);
        var maximo = Math.Max(1, opcoes.PortalMaximoAgendamentosFuturos);
        DomainException.When(ativos >= maximo,
            $"Você já tem {ativos} agendamento(s) futuro(s). Para marcar outro, desmarque um deles ou fale com a clínica.");

        await agendaAppService.AgendarPacienteAsync(idAgenda, idPaciente);
        var atualizado = await agendaRepositorio.ObterDetalhadoPorId(idAgenda) ?? agenda;
        return Mapear(atualizado, agora);
    }

    public async Task<PortalAgendamentoResponse> ConfirmarAsync(int idPaciente, int idAgenda)
    {
        var agenda = await ObterAgendaDoPaciente(idPaciente, idAgenda);
        DomainException.When(agenda.Status != StatusAgendamento.Agendado, "Este agendamento não pode mais ser confirmado.");
        await agendaAppService.ConfirmarAsync(idAgenda);
        var atualizado = await agendaRepositorio.ObterDetalhadoPorId(idAgenda) ?? agenda;
        return Mapear(atualizado, AgoraLocal);
    }

    public async Task CancelarAsync(int idPaciente, int idAgenda)
    {
        var agenda = await ObterAgendaDoPaciente(idPaciente, idAgenda);
        DomainException.When(!PodeCancelar(agenda, AgoraLocal),
            $"Pelo portal, só é possível desmarcar com pelo menos {opcoes.PortalAntecedenciaCancelamentoHoras} horas de antecedência. Fale com a clínica.");
        await agendaAppService.LiberarHorarioAsync(idAgenda);
    }

    private async Task<Agenda> ObterAgendaDoPaciente(int idPaciente, int idAgenda)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(idAgenda);
        // Mesmo erro para "não existe" e "não é seu": não revela agendamentos de outras pessoas.
        DomainException.When(agenda is null || agenda.IdPaciente != idPaciente, "Agendamento não encontrado.");
        return agenda!;
    }

    private bool PodeCancelar(Agenda a, DateTime agora) =>
        a.Status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado
        && a.InicioLocal - agora >= TimeSpan.FromHours(Math.Max(0, opcoes.PortalAntecedenciaCancelamentoHoras));

    private PortalAgendamentoResponse Mapear(Agenda a, DateTime agora) =>
        new(
            a.Id,
            a.InicioLocal,
            a.DuracaoMinutos,
            a.Tipo.ObterNome(),
            a.Medico?.Pessoa?.Nome,
            a.Medico?.Especialidades.Select(e => e.ObterNome()).ToList() ?? new List<string>(),
            a.Status?.ToString(),
            a.Status == StatusAgendamento.Agendado && a.InicioLocal > agora,
            PodeCancelar(a, agora));

    // ------------------------- Exames, documentos e cobranças -------------------------

    private bool ResultadoLiberado(Exame e) =>
        opcoes.PortalLiberarResultados && !e.Cancelado && e.Status is StatusExame.Laudado or StatusExame.Entregue;

    public async Task<List<PortalExameResponse>> ObterExamesAsync(int idPaciente)
    {
        var exames = (await exameRepositorio.ObterPorPaciente(idPaciente)).Where(e => !e.Cancelado).ToList();
        var resultado = new List<PortalExameResponse>();
        foreach (var e in exames)
        {
            var liberado = ResultadoLiberado(e);
            var anexos = liberado
                ? (await anexoRepositorio.ObterResumoPorExame(e.Id))
                    .Select(a => new ExameAnexoResponse(a.Id, a.IdExame, a.NomeArquivo, a.ContentType, a.TamanhoBytes, "clínica", a.DtCriacao))
                    .ToList()
                : new List<ExameAnexoResponse>();
            resultado.Add(new PortalExameResponse(
                e.Id, e.Descricao, e.DtCriacao, e.Status.ToString(), liberado,
                liberado ? e.Resultado : null,
                liberado ? e.DtLaudo : null,
                e.Medico?.Pessoa?.Nome,
                anexos));
        }
        return resultado;
    }

    public async Task<ArquivoResponse?> ObterArquivoExameAsync(int idPaciente, int idExame, int idAnexo)
    {
        var exame = await exameRepositorio.ObterId(idExame);
        if (exame is null || exame.IdPaciente != idPaciente || !ResultadoLiberado(exame)) return null;
        var anexo = await anexoRepositorio.ObterId(idAnexo);
        if (anexo is null || anexo.IdExame != idExame) return null;
        return new ArquivoResponse(anexo.NomeArquivo, anexo.ContentType, anexo.Conteudo);
    }

    public async Task<List<DocumentoResponse>> ObterDocumentosAsync(int idPaciente)
    {
        var documentos = await documentoRepositorio.ObterPorPaciente(idPaciente);
        return documentos.Where(d => !d.Cancelado).Select(d => DocumentoAppService.Mapear(d, opcoes)).ToList();
    }

    public async Task<DocumentoResponse?> ObterDocumentoAsync(int idPaciente, int idDocumento)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(idDocumento);
        if (documento is null || documento.IdPaciente != idPaciente || documento.Cancelado) return null;
        return DocumentoAppService.Mapear(documento, opcoes);
    }

    public async Task<List<CobrancaResponse>> ObterCobrancasAsync(int idPaciente)
    {
        var hoje = AgoraLocal.Date;
        return (await cobrancaRepositorio.ObterPorPaciente(idPaciente))
            .Where(c => c.Status != StatusCobranca.Cancelada)
            .Select(c => FinanceiroAppService.Mapear(c, hoje))
            .ToList();
    }

    public async Task<ReciboResponse?> ObterReciboAsync(int idPaciente, int idCobranca)
    {
        var cobranca = await cobrancaRepositorio.ObterDetalhadoPorId(idCobranca);
        if (cobranca is null || cobranca.IdPaciente != idPaciente || cobranca.Status != StatusCobranca.Paga) return null;
        return new ReciboResponse(FinanceiroAppService.Mapear(cobranca, AgoraLocal.Date), ValorPorExtenso.Converter(cobranca.ValorFinal),
            opcoes.Nome, opcoes.Endereco, opcoes.Telefone, AgoraUtc);
    }
}
