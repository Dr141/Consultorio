using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class AgendaAppService(
    IAgendaRepositorio agendaRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IPacienteRepositorio pacienteRepositorio) : IAgendaAppService
{
    public async Task<List<AgendaResponse>> ObterTodosAsync()
    {
        var agendas = await agendaRepositorio.ObterTodosDetalhado();
        return agendas.Select(MapearResponse).ToList();
    }

    public async Task<AgendaResponse?> ObterPorIdAsync(int id)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id);
        return agenda is null ? null : MapearResponse(agenda);
    }

    public async Task<List<AgendaResponse>> ObterPorMedicoEDataAsync(int idMedico, DateTime data)
    {
        var agendas = await agendaRepositorio.ObterPorMedicoEData(idMedico, data);
        return agendas.Select(MapearResponse).ToList();
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
        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<AgendaResponse> LiberarHorarioAsync(int id)
    {
        var agenda = await agendaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        agenda.LiberarHorario();
        var atualizado = await agendaRepositorio.Atualizar(agenda);
        var detalhado = await agendaRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var agenda = await agendaRepositorio.ObterId(id)
            ?? throw new DomainException($"Horário de agenda com id {id} não encontrado.");

        await agendaRepositorio.Remover(agenda);
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
