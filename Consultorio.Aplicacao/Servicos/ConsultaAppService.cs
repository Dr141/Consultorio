using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
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
        var paciente = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");
        DomainException.When(!paciente.Ativo, "Paciente inativo. Reative o cadastro antes de registrar a consulta.");

        var medico = await medicoRepositorio.ObterId(request.IdMedico)
            ?? throw new DomainException($"Médico com id {request.IdMedico} não encontrado.");
        DomainException.When(!medico.Ativo, "Médico inativo não pode registrar consultas.");

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
                // Registrar a consulta de um horário marcado indica que o atendimento começou.
                if (agenda.Status is StatusAgendamento.Agendado or StatusAgendamento.Confirmado or StatusAgendamento.Aguardando)
                {
                    agenda.IniciarAtendimento();
                }
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        var detalhada = await consultaRepositorio.ObterDetalhadoPorId(salva.Id);
        return MapearResponse(detalhada ?? salva);
    }

    public async Task<ConsultaResponse> AtualizarObservacaoAsync(int id, string observacao, UsuarioAtual? usuario = null)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        GarantirAutoria(consulta, usuario);
        consulta.AtualizarObservacao(observacao);
        var atualizada = await consultaRepositorio.Atualizar(consulta);
        var detalhada = await consultaRepositorio.ObterDetalhadoPorId(atualizada.Id);
        return MapearResponse(detalhada ?? atualizada);
    }

    public async Task<ConsultaResponse> FinalizarAsync(int id, UsuarioAtual? usuario = null)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        GarantirAutoria(consulta, usuario);
        consulta.Finalizar();
        var atualizada = await consultaRepositorio.Atualizar(consulta);

        // Consulta finalizada encerra o atendimento do horário vinculado na fila do dia.
        if (consulta.IdAgenda.HasValue)
        {
            var agenda = await agendaRepositorio.ObterId(consulta.IdAgenda.Value);
            if (agenda?.Status is StatusAgendamento.Aguardando or StatusAgendamento.EmAtendimento)
            {
                agenda.ConcluirAtendimento();
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        return MapearResponse(atualizada);
    }

    public async Task<ConsultaResponse> AnularAsync(int id, string motivo, UsuarioAtual? usuario = null)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        GarantirAutoria(consulta, usuario);
        consulta.Anular(motivo);
        var atualizada = await consultaRepositorio.Atualizar(consulta);
        return MapearResponse(atualizada);
    }

    public async Task<ConsultaResponse> AdicionarAdendoAsync(int id, string texto, UsuarioAtual usuario)
    {
        var consulta = await consultaRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Consulta com id {id} não encontrada.");

        // Qualquer profissional clínico pode complementar; o autor fica registrado no adendo.
        consulta.AdicionarAdendo(texto, usuario.Identificacao);
        var atualizada = await consultaRepositorio.Atualizar(consulta);
        var detalhada = await consultaRepositorio.ObterDetalhadoPorId(atualizada.Id);
        return MapearResponse(detalhada ?? atualizada);
    }

    public Task RemoverAsync(int id) =>
        throw new DomainException("Registros de consulta fazem parte do prontuário e não podem ser excluídos. Use \"Anular\" informando o motivo.");

    /// <summary>
    /// Se o médico da consulta tem login vinculado, só ele (ou um administrador) altera o registro.
    /// Médicos sem login vinculado mantêm o comportamento anterior (qualquer perfil clínico).
    /// </summary>
    private static void GarantirAutoria(Consulta consulta, UsuarioAtual? usuario)
    {
        if (usuario is null || usuario.EhAdministrador) return;
        var medico = consulta.Medico;
        if (medico?.EmailUsuario is null) return;

        DomainException.When(!medico.PertenceAoUsuario(usuario.Email),
            $"Somente o médico responsável ({medico.Pessoa?.Nome ?? "autor da consulta"}) pode alterar esta consulta.");
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
            c.DtCriacao,
            c.Status.ToString(),
            c.DtFinalizacao,
            c.DtAnulacao,
            c.MotivoAnulacao,
            c.Adendos
                .OrderBy(a => a.DtCriacao)
                .Select(a => new ConsultaAdendoResponse(a.Id, a.Texto, a.Autor, a.DtCriacao))
                .ToList()
        );
    }
}
