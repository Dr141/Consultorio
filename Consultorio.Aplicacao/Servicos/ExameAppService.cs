using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class ExameAppService(
    IExameRepositorio exameRepositorio,
    IPacienteRepositorio pacienteRepositorio,
    IAgendaRepositorio agendaRepositorio,
    IMedicoRepositorio? medicoRepositorio = null,
    IExameAnexoRepositorio? anexoRepositorio = null) : IExameAppService
{
    public async Task<List<ExameResponse>> ObterTodosAsync()
    {
        var exames = await exameRepositorio.ObterTodosDetalhado();
        return await MapearComAnexosAsync(exames);
    }

    private async Task<List<ExameResponse>> MapearComAnexosAsync(List<Exame> exames)
    {
        var contagem = anexoRepositorio is null
            ? new Dictionary<int, int>()
            : await anexoRepositorio.ContarPorExames(exames.Select(e => e.Id));
        return exames
            .Select(e => MapearResponse(e) with { QuantidadeAnexos = contagem.GetValueOrDefault(e.Id) })
            .ToList();
    }

    public Task<ExameResponse> RegistrarRealizacaoAsync(int id) => AlterarAsync(id, e => e.RegistrarRealizacao());
    public Task<ExameResponse> RegistrarEntregaAsync(int id) => AlterarAsync(id, e => e.RegistrarEntrega());

    private async Task<ExameResponse> AlterarAsync(int id, Action<Exame> alteracao)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");
        alteracao(exame);
        var atualizado = await exameRepositorio.Atualizar(exame);
        return MapearResponse(atualizado);
    }

    public async Task<ExameAnexoResponse> AdicionarAnexoAsync(int id, string nomeArquivo, byte[] conteudo, string enviadoPor)
    {
        var repositorio = anexoRepositorio ?? throw new DomainException("Anexos de exames não estão disponíveis.");
        var exame = await exameRepositorio.ObterId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");
        DomainException.When(exame.Cancelado, "Exame cancelado não aceita anexos.");

        var anexo = await repositorio.Adicionar(new ExameAnexo(id, nomeArquivo, conteudo, enviadoPor));

        // Resultado anexado a um exame ainda "solicitado" indica que ele foi realizado.
        if (exame.Status == StatusExame.Solicitado)
        {
            exame.RegistrarRealizacao();
            await exameRepositorio.Atualizar(exame);
        }

        return MapearAnexo(anexo);
    }

    public async Task<List<ExameAnexoResponse>> ObterAnexosAsync(int id)
    {
        if (anexoRepositorio is null) return [];
        var anexos = await anexoRepositorio.ObterResumoPorExame(id);
        return anexos.Select(MapearAnexo).ToList();
    }

    public async Task<ArquivoResponse?> ObterArquivoAnexoAsync(int idExame, int idAnexo)
    {
        if (anexoRepositorio is null) return null;
        var anexo = await anexoRepositorio.ObterId(idAnexo);
        if (anexo is null || anexo.IdExame != idExame) return null;
        return new ArquivoResponse(anexo.NomeArquivo, anexo.ContentType, anexo.Conteudo);
    }

    private static ExameAnexoResponse MapearAnexo(ExameAnexo a) =>
        new(a.Id, a.IdExame, a.NomeArquivo, a.ContentType, a.TamanhoBytes, a.EnviadoPor, a.DtCriacao);

    public async Task<ExameResponse?> ObterPorIdAsync(int id)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id);
        return exame is null ? null : MapearResponse(exame);
    }

    public async Task<List<ExameResponse>> ObterPorPacienteAsync(int idPaciente)
    {
        var exames = await exameRepositorio.ObterPorPaciente(idPaciente);
        return await MapearComAnexosAsync(exames);
    }

    public async Task<ExameResponse> AdicionarAsync(ExameRequest request)
    {
        var paciente = await pacienteRepositorio.ObterId(request.IdPaciente)
            ?? throw new DomainException($"Paciente com id {request.IdPaciente} não encontrado.");
        DomainException.When(!paciente.Ativo, "Paciente inativo. Reative o cadastro antes de solicitar exames.");

        if (request.IdAgenda.HasValue && request.IdAgenda.Value > 0)
        {
            _ = await agendaRepositorio.ObterId(request.IdAgenda.Value)
                ?? throw new DomainException($"Agenda com id {request.IdAgenda.Value} não encontrada.");
        }

        if (request.IdMedico.HasValue && request.IdMedico.Value > 0 && medicoRepositorio is not null)
        {
            _ = await medicoRepositorio.ObterId(request.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {request.IdMedico.Value} não encontrado.");
        }

        var exame = new Exame(request.IdPaciente, request.Descricao, request.Resultado, request.IdAgenda, request.IdMedico);
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

        if (request.IdMedico.HasValue && request.IdMedico.Value > 0 && medicoRepositorio is not null)
        {
            _ = await medicoRepositorio.ObterId(request.IdMedico.Value)
                ?? throw new DomainException($"Médico com id {request.IdMedico.Value} não encontrado.");
        }

        exame.Atualizar(request.Descricao, request.Resultado, request.IdMedico);
        var atualizado = await exameRepositorio.Atualizar(exame);
        var detalhado = await exameRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<ExameResponse> RegistrarResultadoAsync(int id, string resultado, int? idMedico = null)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");

        if (idMedico.HasValue && idMedico.Value > 0 && medicoRepositorio is not null)
        {
            _ = await medicoRepositorio.ObterId(idMedico.Value)
                ?? throw new DomainException($"Médico com id {idMedico.Value} não encontrado.");
        }

        exame.RegistrarResultado(resultado, idMedico);
        var atualizado = await exameRepositorio.Atualizar(exame);
        var detalhado = await exameRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public async Task<ExameResponse> CancelarAsync(int id, string motivo)
    {
        var exame = await exameRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Exame com id {id} não encontrado.");

        exame.Cancelar(motivo);

        // O horário reservado para o exame volta a ficar disponível.
        if (exame.IdAgenda.HasValue)
        {
            var agenda = await agendaRepositorio.ObterId(exame.IdAgenda.Value);
            if (agenda is not null)
            {
                agenda.LiberarHorario();
                await agendaRepositorio.Atualizar(agenda);
            }
        }

        var atualizado = await exameRepositorio.Atualizar(exame);
        var detalhado = await exameRepositorio.ObterDetalhadoPorId(atualizado.Id);
        return MapearResponse(detalhado ?? atualizado);
    }

    public Task RemoverAsync(int id) =>
        throw new DomainException("Exames fazem parte do prontuário e não podem ser excluídos. Use \"Cancelar\" informando o motivo.");

    public static ExameResponse MapearResponse(Exame e)
    {
        DateTime? dataHora = null;
        if (e.Agenda is not null)
        {
            dataHora = e.Agenda.Data.Add(e.Agenda.Hora);
        }

        var medico = e.Medico ?? e.Agenda?.Medico;
        int? idMedico = e.IdMedico ?? e.Agenda?.IdMedico;

        return new ExameResponse(
            e.Id,
            e.IdPaciente,
            e.Paciente?.Pessoa?.Nome,
            e.Descricao,
            e.Resultado,
            e.IdAgenda,
            dataHora,
            e.DtCriacao,
            idMedico,
            medico?.Pessoa?.Nome,
            medico?.Crm?.Valor,
            e.Cancelado,
            e.DtCancelamento,
            e.MotivoCancelamento,
            e.Status.ToString(),
            e.DtRealizacao,
            e.DtLaudo,
            e.DtEntrega
        );
    }
}
