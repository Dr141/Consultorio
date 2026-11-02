using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

public class AgendaAppService(
    IAgendaRepositorio agendaRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    ICacheService? cacheService = null) : IAgendaAppService
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

            if (await agendaRepositorio.ExisteConflitoHorario(request.IdMedico.Value, request.Data, request.Hora))
            {
                throw new DomainException($"O médico {medico.Pessoa?.Nome ?? request.IdMedico.Value.ToString()} já possui agendamento para essa data e horário.");
            }
        }

        if (request.IdPaciente.HasValue && request.IdPaciente.Value > 0)
        {
            _ = await pacienteRepositorio.ObterId(request.IdPaciente.Value)
                ?? throw new DomainException($"Paciente com id {request.IdPaciente.Value} não encontrado.");
        }

        var agenda = new Agenda(request.Data, request.Hora, request.IdMedico, request.IdPaciente);
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

        agenda.AtualizarHorario(request.Data, request.Hora, request.IdMedico);
        var atualizado = await agendaRepositorio.Atualizar(agenda);

        await InvalidarCacheAsync(id, atualizado.IdMedico, atualizado.Data);

        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<AgendaResponse> AgendarPacienteAsync(int id, int idPaciente)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        _ = await pacienteRepositorio.ObterId(idPaciente)
            ?? throw new DomainException($"Paciente com id {idPaciente} não encontrado.");

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
            a.IdConsulta,
            a.IdExame,
            a.DtCriacao
        );
}
