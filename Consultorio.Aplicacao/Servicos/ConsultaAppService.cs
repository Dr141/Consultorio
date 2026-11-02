using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class ConsultaAppService(
    IConsultaRepositorio consultaRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IMedicoRepositorio medicoRepositorio,
    IAgendaRepositorio agendaRepositorio) : IConsultaAppService
{
    public async Task<List<ConsultaResponse>> ObterTodosAsync()
    {
        var consultas = await consultaRepositorio.ObterTodosDetalhado();
        return consultas.Select(MapearResponse).ToList();
    }

    public async Task<ConsultaResponse?> ObterPorIdAsync(int id)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id);
        return consulta is null ? null : MapearResponse(consulta);
    }

    public async Task<List<ConsultaResponse>> ObterPorPacienteAsync(int idPaciente)
    {
        var consultas = await consultaRepositorio.ObterPorPaciente(idPaciente);
        return consultas.Select(MapearResponse).ToList();
    }

    public async Task<List<ConsultaResponse>> ObterPorMedicoAsync(int idMedico)
    {
        var consultas = await consultaRepositorio.ObterPorMedico(idMedico);
        return consultas.Select(MapearResponse).ToList();
    }

    public async Task<ConsultaResponse> AdicionarAsync(ConsultaRequest request)
    {
        _ = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");

        _ = await medicoRepositorio.ObterId(request.IdMedico)
            ?? throw new DomainException($"Médico com id {request.IdMedico} não encontrado.");

        if (request.IdAgenda.HasValue && request.IdAgenda.Value > 0)
        {
            var agenda = await agendaRepositorio.ObterId(request.IdAgenda.Value)
                ?? throw new DomainException($"Agenda com id {request.IdAgenda.Value} não encontrada.");
        }

        var consulta = new Consulta(request.IdPaciente, request.IdMedico, request.Observacao, request.IdAgenda);
        var salva = await consultaRepositorio.Adicionar(consulta);

        if (request.IdAgenda.HasValue && request.IdAgenda.Value > 0)
        {
            var agenda = await agendaRepositorio.ObterId(request.IdAgenda.Value);
            if (agenda is not null)
            {
                agenda.VincularConsulta(salva.Id);
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        var detalhada = await consultaRepositorio.ObterDetalhadoPorId(salva.Id);
        return MapearResponse(detalhada ?? salva);
    }

    public async Task<ConsultaResponse> AtualizarObservacaoAsync(int id, string observacao)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        consulta.AtualizarObservacao(observacao);
        var atualizada = await consultaRepositorio.Atualizar(consulta);
        var detalhada = await consultaRepositorio.ObterDetalhadoPorId(atualizada.Id);
        return MapearResponse(detalhada ?? atualizada);
    }

    public async Task RemoverAsync(int id)
    {
        var consulta = await consultaRepositorio.ObterId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        if (consulta.IdAgenda.HasValue)
        {
            var agenda = await agendaRepositorio.ObterId(consulta.IdAgenda.Value);
            if (agenda is not null)
            {
                agenda.LiberarHorario();
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        await consultaRepositorio.Remover(consulta);
    }

    public static ConsultaResponse MapearResponse(Consulta c)
    {
        DateTime? dataHora = null;
        if (c.Agenda is not null)
        {
            dataHora = c.Agenda.Data.Add(c.Agenda.Hora);
        }

        return new ConsultaResponse(
            c.Id,
            c.IdPaciente,
            c.Paciente?.Pessoa?.Nome,
            c.IdMedico,
            c.Medico?.Pessoa?.Nome,
            c.Observacao,
            c.IdAgenda,
            dataHora,
            c.DtCriacao
        );
    }
}
