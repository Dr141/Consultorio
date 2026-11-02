using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class ExameAppService(
    IExameRepositorio exameRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IAgendaRepositorio agendaRepositorio) : IExameAppService
{
    public async Task<List<ExameResponse>> ObterTodosAsync()
    {
        var exames = await exameRepositorio.ObterTodosDetalhado();
        return exames.Select(MapearResponse).ToList();
    }

    public async Task<ExameResponse?> ObterPorIdAsync(int id)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id);
        return exame is null ? null : MapearResponse(exame);
    }

    public async Task<List<ExameResponse>> ObterPorPacienteAsync(int idPaciente)
    {
        var exames = await exameRepositorio.ObterPorPaciente(idPaciente);
        return exames.Select(MapearResponse).ToList();
    }

    public async Task<ExameResponse> AdicionarAsync(ExameRequest request)
    {
        _ = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");

        if (request.IdAgenda.HasValue && request.IdAgenda.Value > 0)
        {
            _ = await agendaRepositorio.ObterId(request.IdAgenda.Value)
                ?? throw new DomainException($"Agenda com id {request.IdAgenda.Value} não encontrada.");
        }

        var exame = new Exame(request.IdPaciente, request.Descricao, request.Resultado, request.IdAgenda);
        var salvo = await exameRepositorio.Adicionar(exame);

        if (request.IdAgenda.HasValue && request.IdAgenda.Value > 0)
        {
            var agenda = await agendaRepositorio.ObterId(request.IdAgenda.Value);
            if (agenda is not null)
            {
                agenda.VincularExame(salvo.Id);
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        var detalhado = await exameRepositorio.ObterDetalhadoPorId(salvo.Id);
        return MapearResponse(detalhado ?? salvo);
    }

    public async Task<ExameResponse> AtualizarAsync(int id, ExameRequest request)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");

        exame.Atualizar(request.Descricao, request.Resultado);
        var atualizado = await exameRepositorio.Atualizar(exame);
        var detalhado = await exameRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<ExameResponse> RegistrarResultadoAsync(int id, string resultado)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");

        exame.RegistrarResultado(resultado);
        var atualizado = await exameRepositorio.Atualizar(exame);
        var detalhado = await exameRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var exame = await exameRepositorio.ObterId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");

        if (exame.IdAgenda.HasValue)
        {
            var agenda = await agendaRepositorio.ObterId(exame.IdAgenda.Value);
            if (agenda is not null)
            {
                agenda.LiberarHorario();
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        await exameRepositorio.Remover(exame);
    }

    public static ExameResponse MapearResponse(Exame e)
    {
        DateTime? dataHora = null;
        if (e.Agenda is not null)
        {
            dataHora = e.Agenda.Data.Add(e.Agenda.Hora);
        }

        return new ExameResponse(
            e.Id,
            e.IdPaciente,
            e.Paciente?.Pessoa?.Nome,
            e.Descricao,
            e.Resultado,
            e.IdAgenda,
            dataHora,
            e.DtCriacao
        );
    }
}
