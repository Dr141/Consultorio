using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Servicos;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

public class AgendaAppService(
    IAgendaRepositorio agendaRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    ICacheService? cacheService = null,
    IBloqueioAgendaRepositorio? bloqueioRepositorio = null) : IAgendaAppService
{
    private const string CacheKeyTodos = "agenda:todos";

    public async Task<List<AgendaResponse>> ObterTodosAsync()
    {
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<List<AgendaResponse>>(CacheKeyTodos);
            if (emCache is not null)
                return emCache;
        }

        var agendas = await agendaRepositorio.ObterTodosDetalhado();
        var response = agendas.Select(MapearResponse).ToList();

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(CacheKeyTodos, response, TimeSpan.FromMinutes(10));
        }

        return response;
    }

    public async Task<AgendaResponse?> ObterPorIdAsync(int id)
    {
        string cacheKey = $"agenda:{id}";
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<AgendaResponse>(cacheKey);
            if (emCache is not null)
                return emCache;
        }

        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id);
        if (agenda is null) return null;

        var response = MapearResponse(agenda);

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        }

        return response;
    }

    public async Task<List<AgendaResponse>> ObterPorMedicoEDataAsync(int idMedico, DateTime data)
    {
        string cacheKey = $"agenda:medico:{idMedico}:{data:yyyyMMdd}";
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<List<AgendaResponse>>(cacheKey);
            if (emCache is not null)
                return emCache;
        }

        var agendas = await agendaRepositorio.ObterPorMedicoEData(idMedico, data);
        var response = agendas.Select(MapearResponse).ToList();

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(cacheKey, response, TimeSpan.FromMinutes(10));
        }

        return response;
    }

    public async Task<List<AgendaResponse>> ObterPorPacienteAsync(int idPaciente)
    {
        var agendas = await agendaRepositorio.ObterPorPaciente(idPaciente);
        return agendas.Select(MapearResponse).ToList();
    }

    public async Task<AgendaResponse> AdicionarAsync(AgendaRequest request)
    {
        if (request.IdMedico.HasValue && request.IdMedico.Value > 0)
        {
            var medico = await medicoRepositorio.ObterId(request.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {request.IdMedico.Value} não encontrado.");
            DomainException.When(!medico.Ativo, "Médico inativo não pode receber novos horários.");

            if (await agendaRepositorio.ExisteConflitoHorario(request.IdMedico.Value, request.Data, request.Hora))
            {
                throw new DomainException($"O médico {medico.Pessoa?.Nome ?? request.IdMedico.Value.ToString()} já possui agendamento para essa data e horário.");
            }
        }

        await GarantirSemBloqueioAsync(request.Data, request.IdMedico);

        if (request.IdPaciente.HasValue && request.IdPaciente.Value > 0)
        {
            await ObterPacienteAtivoAsync(request.IdPaciente.Value);
        }

        var agenda = new Agenda(request.Data, request.Hora, request.IdMedico, request.IdPaciente,
            request.Tipo ?? TipoAtendimento.Consulta, request.DuracaoMinutos ?? 30);
        var salvo = await agendaRepositorio.Adicionar(agenda);

        await InvalidarCacheAsync(salvo.Id, salvo.IdMedico, salvo.Data);

        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(salvo.Id);
        return MapearResponse(detalhado ?? salvo);
    }

    public async Task<AgendaResponse> AtualizarAsync(int id, AgendaRequest request)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        if (request.IdMedico.HasValue && request.IdMedico.Value > 0)
        {
            var medico = await medicoRepositorio.ObterId(request.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {request.IdMedico.Value} não encontrado.");

            if (await agendaRepositorio.ExisteConflitoHorario(request.IdMedico.Value, request.Data, request.Hora, excluirId: id))
            {
                throw new DomainException($"O médico {medico.Pessoa?.Nome ?? request.IdMedico.Value.ToString()} já possui agendamento para essa data e horário.");
            }
        }

        if (agenda.Data.Date != request.Data.Date)
        {
            await GarantirSemBloqueioAsync(request.Data, request.IdMedico ?? agenda.IdMedico);
        }

        agenda.AtualizarHorario(request.Data, request.Hora, request.IdMedico);
        if (request.Tipo.HasValue || request.DuracaoMinutos.HasValue)
        {
            agenda.DefinirTipo(request.Tipo ?? agenda.Tipo, request.DuracaoMinutos ?? agenda.DuracaoMinutos);
        }
        var atualizado = await agendaRepositorio.Atualizar(agenda);

        await InvalidarCacheAsync(id, atualizado.IdMedico, atualizado.Data);

        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<AgendaResponse> AgendarPacienteAsync(int id, int idPaciente)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        await ObterPacienteAtivoAsync(idPaciente);
        await GarantirSemBloqueioAsync(agenda.Data, agenda.IdMedico);

        agenda.AgendarPaciente(idPaciente);
        var atualizado = await agendaRepositorio.Atualizar(agenda);

        await InvalidarCacheAsync(id, atualizado.IdMedico, atualizado.Data);

        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<AgendaResponse> LiberarHorarioAsync(int id)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        agenda.LiberarHorario();
        var atualizado = await agendaRepositorio.Atualizar(agenda);

        await InvalidarCacheAsync(id, atualizado.IdMedico, atualizado.Data);

        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var agenda = await agendaRepositorio.ObterId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        await agendaRepositorio.Remover(agenda);
        await InvalidarCacheAsync(id, agenda.IdMedico, agenda.Data);
    }

    public async Task<List<AgendaResponse>> ObterDoDiaAsync(DateTime data, int? idMedico = null)
    {
        var agendas = await agendaRepositorio.ObterPorPeriodo(data, data, idMedico);
        return agendas
            .Where(a => a.Status.HasValue)
            .OrderBy(a => a.Hora)
            .Select(MapearResponse)
            .ToList();
    }

    public Task<AgendaResponse> ConfirmarAsync(int id) => AlterarSituacaoAsync(id, a => a.Confirmar());
    public Task<AgendaResponse> RegistrarChegadaAsync(int id) => AlterarSituacaoAsync(id, a => a.RegistrarChegada());
    public Task<AgendaResponse> IniciarAtendimentoAsync(int id) => AlterarSituacaoAsync(id, a => a.IniciarAtendimento());
    public Task<AgendaResponse> ConcluirAtendimentoAsync(int id) => AlterarSituacaoAsync(id, a => a.ConcluirAtendimento());
    public Task<AgendaResponse> RegistrarFaltaAsync(int id) => AlterarSituacaoAsync(id, a => a.RegistrarFalta());

    private async Task<AgendaResponse> AlterarSituacaoAsync(int id, Action<Agenda> alteracao)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        alteracao(agenda);
        var atualizado = await agendaRepositorio.Atualizar(agenda);
        await InvalidarCacheAsync(id, atualizado.IdMedico, atualizado.Data);
        return MapearResponse(atualizado);
    }

    public async Task<GradeResponse> GerarGradeAsync(GradeRequest request)
    {
        var medico = await medicoRepositorio.ObterId(request.IdMedico)
            ?? throw new DomainException($"Médico com id {request.IdMedico} não encontrado.");
        DomainException.When(!medico.Ativo, "Médico inativo não pode receber novos horários.");
        DomainException.When(request.DiasSemana.Any(d => d < 0 || d > 6), "Dia da semana inválido.");

        var parametros = new ParametrosGrade(
            request.DataInicio, request.DataFim,
            request.DiasSemana.Distinct().Select(d => (DayOfWeek)d).ToList(),
            request.HoraInicio, request.HoraFim, request.IntervaloMinutos,
            request.PausaInicio, request.PausaFim);
        GradeHorarios.Validar(parametros);

        List<BloqueioAgenda> bloqueios = bloqueioRepositorio is null
            ? new List<BloqueioAgenda>()
            : await bloqueioRepositorio.ObterNoPeriodo(request.DataInicio, request.DataFim, request.IdMedico);
        var existentes = await agendaRepositorio.ObterPorPeriodo(request.DataInicio, request.DataFim, request.IdMedico);

        var (novos, jaExistiam, bloqueados) = GradeHorarios.Gerar(
            parametros, request.IdMedico, bloqueios, existentes.Select(e => (e.Data, e.Hora)));

        var tipo = request.Tipo ?? TipoAtendimento.Consulta;
        var agendas = novos.Select(h => new Agenda(h.Data, h.Hora, request.IdMedico, null, tipo, request.IntervaloMinutos)).ToList();
        if (agendas.Count > 0)
        {
            await agendaRepositorio.AdicionarVarios(agendas);
        }

        await InvalidarCacheAsync(0, null, request.DataInicio);
        return new GradeResponse(agendas.Count, jaExistiam, bloqueados);
    }

    public async Task<List<BloqueioResponse>> ObterBloqueiosAsync()
    {
        if (bloqueioRepositorio is null) return [];
        var bloqueios = await bloqueioRepositorio.ObterAPartirDe(DateTime.UtcNow.Date.AddDays(-30));
        return bloqueios.Select(MapearBloqueio).ToList();
    }

    public async Task<BloqueioCriadoResponse> CriarBloqueioAsync(BloqueioRequest request)
    {
        var repositorioBloqueios = bloqueioRepositorio
            ?? throw new DomainException("Bloqueios de agenda não estão disponíveis.");

        if (request.IdMedico.HasValue)
        {
            _ = await medicoRepositorio.ObterId(request.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {request.IdMedico.Value} não encontrado.");
        }

        var bloqueio = new BloqueioAgenda(request.IdMedico, request.DataInicio, request.DataFim, request.Motivo);
        var salvo = await repositorioBloqueios.Adicionar(bloqueio);

        // Horários livres no período deixam de existir; os que têm paciente precisam ser remarcados.
        var noPeriodo = await agendaRepositorio.ObterPorPeriodo(request.DataInicio, request.DataFim, request.IdMedico);
        var livres = noPeriodo.Where(a => a.Disponivel).ToList();
        var conflitos = noPeriodo.Where(a => !a.Disponivel).ToList();
        if (livres.Count > 0)
        {
            await agendaRepositorio.RemoverVarios(livres);
        }

        await InvalidarCacheAsync(0, null, request.DataInicio);
        var completo = (await repositorioBloqueios.ObterNoPeriodo(salvo.DataInicio, salvo.DataFim))
            .FirstOrDefault(b => b.Id == salvo.Id) ?? salvo;

        return new BloqueioCriadoResponse(MapearBloqueio(completo), livres.Count, conflitos.Select(MapearResponse).ToList());
    }

    public async Task RemoverBloqueioAsync(int id)
    {
        var repositorioBloqueios = bloqueioRepositorio
            ?? throw new DomainException("Bloqueios de agenda não estão disponíveis.");
        var bloqueio = await repositorioBloqueios.ObterId(id)
            ?? throw new DomainException($"Bloqueio com id {id} não encontrado.");
        await repositorioBloqueios.Remover(bloqueio);
    }

    public async Task<AgendaResponse?> ObterPorTokenAsync(Guid token)
    {
        if (token == Guid.Empty) return null;
        var agenda = await agendaRepositorio.ObterPorToken(token);
        return agenda is null ? null : MapearResponse(agenda);
    }

    public async Task<ConfirmacaoPacienteResponse> ResponderConfirmacaoAsync(Guid token, bool confirmar)
    {
        var agenda = token == Guid.Empty ? null : await agendaRepositorio.ObterPorToken(token);
        if (agenda is null)
        {
            return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.LinkInvalido,
                "Este link não é mais válido. O horário pode ter sido desmarcado ou alterado. Entre em contato com a clínica.", null);
        }

        var resposta = MapearResponse(agenda);
        // Data do horário guardada como dia (00:00 UTC); tolera um dia para não depender do fuso.
        if (agenda.Data.Date < DateTime.UtcNow.Date.AddDays(-1))
        {
            return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.NaoPermitido,
                "Este atendimento já passou. Para um novo horário, entre em contato com a clínica.", resposta);
        }

        if (confirmar)
        {
            if (agenda.Status == StatusAgendamento.Confirmado)
            {
                return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.JaConfirmado, "Sua presença já estava confirmada. Até lá!", resposta);
            }
            if (agenda.Status != StatusAgendamento.Agendado)
            {
                return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.NaoPermitido,
                    "Não foi possível confirmar por este link. Entre em contato com a clínica.", resposta);
            }

            agenda.Confirmar();
            var confirmada = await agendaRepositorio.Atualizar(agenda);
            await InvalidarCacheAsync(confirmada.Id, confirmada.IdMedico, confirmada.Data);
            return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.Confirmado, "Presença confirmada. Obrigado!", MapearResponse(confirmada));
        }

        if (agenda.Status is not (StatusAgendamento.Agendado or StatusAgendamento.Confirmado))
        {
            return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.NaoPermitido,
                "Não foi possível desmarcar por este link. Entre em contato com a clínica.", resposta);
        }

        agenda.LiberarHorario();
        var liberada = await agendaRepositorio.Atualizar(agenda);
        await InvalidarCacheAsync(liberada.Id, liberada.IdMedico, liberada.Data);
        return new ConfirmacaoPacienteResponse(ResultadoConfirmacao.Desmarcado,
            "Seu horário foi desmarcado. Se quiser remarcar, entre em contato com a clínica.", resposta);
    }

    private async Task<Paciente> ObterPacienteAtivoAsync(int idPaciente)
    {
        var paciente = await pacienteRepositorio.ObterId(idPaciente)
            ?? throw new DomainException($"Paciente com id {idPaciente} não encontrado.");
        DomainException.When(!paciente.Ativo, "Paciente inativo. Reative o cadastro antes de agendar.");
        return paciente;
    }

    private async Task GarantirSemBloqueioAsync(DateTime data, int? idMedico)
    {
        if (bloqueioRepositorio is null) return;
        var bloqueios = await bloqueioRepositorio.ObterNoPeriodo(data, data, idMedico);
        var bloqueio = bloqueios.FirstOrDefault(b => b.Abrange(data, idMedico));
        if (bloqueio is not null)
        {
            var quem = bloqueio.IdMedico is null ? "A clínica" : (bloqueio.Medico?.Pessoa?.Nome ?? "O médico");
            throw new DomainException($"{quem} está com a agenda bloqueada em {data:dd/MM/yyyy}: {bloqueio.Motivo}.");
        }
    }

    public static BloqueioResponse MapearBloqueio(BloqueioAgenda b) =>
        new(b.Id, b.IdMedico, b.Medico?.Pessoa?.Nome, b.DataInicio, b.DataFim, b.Motivo, b.DtCriacao);

    private async Task InvalidarCacheAsync(int id, int? idMedico, DateTime data)
    {
        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"agenda:{id}");
            if (idMedico.HasValue)
            {
                await cacheService.RemoverAsync($"agenda:medico:{idMedico.Value}:{data:yyyyMMdd}");
            }
        }
    }

    public static AgendaResponse MapearResponse(Agenda a) =>
        new(
            a.Id,
            a.Data,
            a.Hora,
            a.IdMedico,
            a.Medico?.Pessoa?.Nome,
            a.IdPaciente,
            a.Paciente?.Pessoa?.Nome,
            a.Disponivel,
            a.Consulta?.Id ?? a.IdConsulta,
            a.Exame?.Id ?? a.IdExame,
            a.DtCriacao,
            a.Status?.ToString(),
            a.Tipo.ToString(),
            a.DuracaoMinutos,
            a.DtConfirmacao,
            a.DtChegada,
            a.DtInicioAtendimento,
            a.DtFimAtendimento,
            a.LembreteEnviadoEm,
            a.Paciente?.Pessoa?.Telefone?.Formatado(),
            a.Paciente?.Pessoa?.Email
        );
}
