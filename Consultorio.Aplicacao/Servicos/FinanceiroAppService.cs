using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Cobranças de atendimentos (particular e convênio), recebimentos, estornos e recibos.
/// O valor sugerido vem da tabela de preços: preço do convênio quando o paciente tem carteirinha válida,
/// senão o preço particular.
/// </summary>
public class FinanceiroAppService(
    ICobrancaRepositorio cobrancaRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IAgendaRepositorio agendaRepositorio,
    ITabelaPrecoRepositorio tabelaPrecoRepositorio,
    IConvenioRepositorio convenioRepositorio,
    OpcoesClinica opcoes,
    TimeProvider relogio) : IFinanceiroAppService
{
    private DateTime HojeLocal => TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, opcoes.ObterFuso()).Date;

    private DateTime ParaLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), opcoes.ObterFuso());

    public async Task<List<CobrancaResponse>> ObterAsync(DateTime? inicio, DateTime? fim, string? status = null, int? idPaciente = null)
    {
        var hoje = HojeLocal;
        var ini = (inicio ?? hoje.AddDays(-30)).Date;
        var f = (fim ?? hoje).Date;
        DomainException.When(ini > f, "A data inicial não pode ser posterior à final.");
        DomainException.When((f - ini).TotalDays > 366, "Consulte no máximo um ano por vez.");

        StatusCobranca? filtro = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            DomainException.When(!Enum.TryParse<StatusCobranca>(status, true, out var situacao) || !Enum.IsDefined(situacao),
                "Situação de cobrança inválida.");
            filtro = situacao;
        }

        var cobrancas = await cobrancaRepositorio.ObterPorPeriodo(ini, f, filtro, idPaciente);
        return cobrancas
            .Where(c => ParaLocal(c.DtCriacao).Date >= ini && ParaLocal(c.DtCriacao).Date <= f)
            .Select(c => Mapear(c, hoje))
            .ToList();
    }

    public async Task<List<CobrancaResponse>> ObterPendentesAsync()
    {
        var hoje = HojeLocal;
        return (await cobrancaRepositorio.ObterPendentes()).Select(c => Mapear(c, hoje)).ToList();
    }

    public async Task<CobrancaResponse?> ObterPorIdAsync(int id)
    {
        var cobranca = await cobrancaRepositorio.ObterDetalhadoPorId(id);
        return cobranca is null ? null : Mapear(cobranca, HojeLocal);
    }

    public async Task<CobrancaResponse> CriarAsync(CobrancaRequest request, UsuarioAtual usuario)
    {
        var paciente = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");
        DomainException.When(!paciente.Ativo, "Paciente inativo. Reative o cadastro antes de lançar cobranças.");

        if (request.IdConvenio.HasValue)
        {
            DomainException.When(await convenioRepositorio.ObterId(request.IdConvenio.Value) is null, "Convênio não encontrado.");
        }
        if (request.IdAgenda.HasValue)
        {
            var existentes = await cobrancaRepositorio.AgendasComCobranca([request.IdAgenda.Value]);
            DomainException.When(existentes.Count > 0, "Este atendimento já tem uma cobrança.");
        }

        var vencimento = request.DtVencimento?.Date ?? HojeLocal;
        var cobranca = new Cobranca(paciente.Id, request.Descricao, request.Valor, vencimento, usuario.Identificacao,
            request.IdAgenda, request.IdConvenio);
        var salva = await cobrancaRepositorio.Adicionar(cobranca);
        return await Recarregar(salva.Id, salva);
    }

    public async Task<CobrancaResponse> GerarDoAtendimentoAsync(int idAgenda, UsuarioAtual usuario)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(idAgenda)
            ?? throw new DomainException($"Atendimento com id {idAgenda} não encontrado.");
        DomainException.When(agenda.IdPaciente is null, "Este horário não tem paciente.");
        DomainException.When(agenda.Status == StatusAgendamento.Faltou, "O paciente faltou a este atendimento.");
        DomainException.When((await cobrancaRepositorio.AgendasComCobranca([idAgenda])).Count > 0, "Este atendimento já tem uma cobrança.");

        var cobranca = await MontarCobrancaAsync(agenda, usuario.Identificacao)
            ?? throw new DomainException($"Cadastre o valor de \"{agenda.Tipo.ObterNome()}\" na tabela de preços (particular ou do convênio do paciente).");
        var salva = await cobrancaRepositorio.Adicionar(cobranca);
        return await Recarregar(salva.Id, salva);
    }

    public async Task<int> GerarCobrancasDeAtendimentosAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default)
    {
        if (!opcoes.GerarCobrancaAutomatica) return 0;

        var atendidos = (await agendaRepositorio.ObterPorPeriodo(inicio, fim))
            .Where(a => a.Status == StatusAgendamento.Atendido && a.IdPaciente.HasValue)
            .ToList();
        if (atendidos.Count == 0) return 0;

        var jaCobrados = await cobrancaRepositorio.AgendasComCobranca(atendidos.Select(a => a.Id));
        var criadas = 0;
        foreach (var agenda in atendidos.Where(a => !jaCobrados.Contains(a.Id)))
        {
            if (cancellationToken.IsCancellationRequested) break;
            var cobranca = await MontarCobrancaAsync(agenda, "sistema");
            if (cobranca is null) continue;
            await cobrancaRepositorio.Adicionar(cobranca);
            criadas++;
        }
        return criadas;
    }

    /// <summary>Cobrança sugerida pela tabela de preços; null quando não há valor cadastrado (ou é zero).</summary>
    private async Task<Cobranca?> MontarCobrancaAsync(Agenda agenda, string registradoPor)
    {
        var paciente = agenda.Paciente ?? await pacienteRepositorio.ObterId(agenda.IdPaciente!.Value);
        if (paciente is null) return null;

        var hoje = HojeLocal;
        TabelaPreco? preco = null;
        int? idConvenio = null;
        if (paciente.IdConvenio.HasValue && !paciente.CarteirinhaVencida(agenda.Data))
        {
            preco = await tabelaPrecoRepositorio.ObterPor(agenda.Tipo, paciente.IdConvenio);
            if (preco is { Valor: > 0 }) idConvenio = paciente.IdConvenio;
        }
        if (idConvenio is null)
        {
            preco = await tabelaPrecoRepositorio.ObterPor(agenda.Tipo, null);
        }
        if (preco is null || preco.Valor <= 0) return null;

        var medico = agenda.Medico?.Pessoa?.Nome;
        var descricao = $"{agenda.Tipo.ObterNome()} em {agenda.Data:dd/MM/yyyy}" + (medico is null ? "" : $" — {medico}");
        if (descricao.Length > 200) descricao = descricao[..200];
        var vencimento = idConvenio.HasValue ? hoje.AddDays(Math.Max(0, opcoes.PrazoPagamentoConvenioDias)) : hoje;

        return new Cobranca(agenda.IdPaciente!.Value, descricao, preco.Valor, vencimento, registradoPor, agenda.Id, idConvenio);
    }

    public async Task<CobrancaResponse> RegistrarPagamentoAsync(int id, PagamentoRequest request, UsuarioAtual usuario)
    {
        var cobranca = await Obter(id);
        if (request.Desconto is > 0) cobranca.AplicarDesconto(request.Desconto.Value);

        DateTime? quando = null;
        if (request.DataPagamento.HasValue)
        {
            // Data informada como dia local: registra ao meio-dia local para não "mudar de dia" com o fuso.
            var local = DateTime.SpecifyKind(request.DataPagamento.Value.Date.AddHours(12), DateTimeKind.Unspecified);
            quando = TimeZoneInfo.ConvertTimeToUtc(local, opcoes.ObterFuso());
            if (quando > relogio.GetUtcNow().UtcDateTime) quando = relogio.GetUtcNow().UtcDateTime;
        }

        cobranca.RegistrarPagamento(request.Forma, usuario.Identificacao, quando);
        await cobrancaRepositorio.Atualizar(cobranca);
        return Mapear(cobranca, HojeLocal);
    }

    public async Task<CobrancaResponse> CancelarAsync(int id, string motivo)
    {
        var cobranca = await Obter(id);
        cobranca.Cancelar(motivo);
        await cobrancaRepositorio.Atualizar(cobranca);
        return Mapear(cobranca, HojeLocal);
    }

    public async Task<CobrancaResponse> EstornarAsync(int id, string motivo)
    {
        var cobranca = await Obter(id);
        cobranca.Estornar(motivo);
        await cobrancaRepositorio.Atualizar(cobranca);
        return Mapear(cobranca, HojeLocal);
    }

    public async Task<ReciboResponse> ObterReciboAsync(int id)
    {
        var cobranca = await Obter(id);
        DomainException.When(cobranca.Status != StatusCobranca.Paga, "O recibo só pode ser emitido para cobranças pagas.");
        return new ReciboResponse(Mapear(cobranca, HojeLocal), ValorPorExtenso.Converter(cobranca.ValorFinal),
            opcoes.Nome, opcoes.Endereco, opcoes.Telefone, relogio.GetUtcNow().UtcDateTime);
    }

    public async Task<ResumoFinanceiroResponse> ObterResumoAsync(DateTime inicio, DateTime fim)
    {
        var fuso = opcoes.ObterFuso();
        var iniUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(inicio.Date, DateTimeKind.Unspecified), fuso);
        var fimUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(fim.Date.AddDays(1), DateTimeKind.Unspecified), fuso);

        var pagas = await cobrancaRepositorio.ObterPagasNoPeriodo(iniUtc, fimUtc);
        var pendentes = await cobrancaRepositorio.ObterPendentes();
        return MontarResumo(pagas, pendentes, HojeLocal);
    }

    public static ResumoFinanceiroResponse MontarResumo(List<Cobranca> pagasNoPeriodo, List<Cobranca> pendentes, DateTime hoje)
    {
        var recebidas = pagasNoPeriodo.Where(c => c.Status == StatusCobranca.Paga).ToList();
        var recebido = recebidas.Sum(c => c.ValorFinal);
        var estornado = pagasNoPeriodo.Where(c => c.Status == StatusCobranca.Estornada).Sum(c => c.ValorFinal);

        var porForma = recebidas
            .GroupBy(c => c.FormaPagamento)
            .Select(g => new ItemIndicador(g.Key?.ObterNome() ?? "Não informado", g.Count(), g.Sum(c => c.ValorFinal)))
            .OrderByDescending(i => i.Valor)
            .ToList();
        var porConvenio = recebidas
            .GroupBy(c => c.Convenio?.Nome ?? "Particular")
            .Select(g => new ItemIndicador(g.Key, g.Count(), g.Sum(c => c.ValorFinal)))
            .OrderByDescending(i => i.Valor)
            .ToList();

        return new ResumoFinanceiroResponse(
            recebido,
            recebidas.Count,
            recebidas.Count == 0 ? 0 : Math.Round(recebido / recebidas.Count, 2),
            pendentes.Sum(c => c.ValorFinal),
            pendentes.Where(c => c.Vencida(hoje)).Sum(c => c.ValorFinal),
            estornado,
            porForma,
            porConvenio);
    }

    private async Task<Cobranca> Obter(int id) =>
        await cobrancaRepositorio.ObterDetalhadoPorId(id) ?? throw new DomainException($"Cobrança com id {id} não encontrada.");

    private async Task<CobrancaResponse> Recarregar(int id, Cobranca fallback) =>
        Mapear(await cobrancaRepositorio.ObterDetalhadoPorId(id) ?? fallback, HojeLocal);

    public static CobrancaResponse Mapear(Cobranca c, DateTime hoje) =>
        new(
            c.Id,
            c.IdPaciente,
            c.Paciente?.Pessoa?.Nome ?? string.Empty,
            c.Paciente?.Pessoa?.Cpf?.Formatado(),
            c.IdAgenda,
            c.IdConvenio,
            c.Convenio?.Nome,
            c.Descricao,
            c.Valor,
            c.Desconto,
            c.ValorFinal,
            c.Status.ToString(),
            c.DtVencimento,
            c.Vencida(hoje),
            c.FormaPagamento?.ToString(),
            c.FormaPagamento?.ObterNome(),
            c.DtPagamento,
            c.RecebidoPor,
            c.DtCancelamento,
            c.MotivoCancelamento,
            c.RegistradoPor,
            c.DtCriacao);
}
