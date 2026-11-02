using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

public class PacienteAppService(
    IPacienteRepositorio pacienteRepositorio,
    IPessoaRepositorio pessoaRepositorio) : IPacienteAppService
{
    public async Task<List<PacienteResponse>> ObterTodosAsync()
    {
        var pacientes = await pacienteRepositorio.ObterTodosDetalhado();
        return pacientes.Select(MapearResponse).ToList();
    }

    public async Task<PacienteResponse?> ObterPorIdAsync(int id)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id);
        return paciente is null ? null : MapearResponse(paciente);
    }

    public async Task<PacienteResponse> AdicionarAsync(PacienteRequest request)
    {
        Pessoa pessoa;

        if (request.IdPessoa.HasValue && request.IdPessoa.Value > 0)
        {
            pessoa = await pessoaRepositorio.ObterId(request.IdPessoa.Value)
                ?? throw new DomainException($"Pessoa com id {request.IdPessoa.Value} não encontrada.");

            var pacienteExistente = await pacienteRepositorio.ObterPorIdPessoa(request.IdPessoa.Value);
            if (pacienteExistente is not null)
            {
                throw new DomainException($"A pessoa {pessoa.Nome} já está cadastrada como paciente.");
            }
        }
        else
        {
            DomainException.When(string.IsNullOrWhiteSpace(request.Nome), "Nome é obrigatório.");
            DomainException.When(string.IsNullOrWhiteSpace(request.Cpf), "CPF é obrigatório.");
            DomainException.When(!request.DtNasc.HasValue, "Data de nascimento é obrigatória.");
            DomainException.When(string.IsNullOrWhiteSpace(request.Telefone), "Telefone é obrigatório.");

            var cpfVo = new Cpf(request.Cpf!);
            if (await pessoaRepositorio.ExisteCpf(cpfVo.Valor))
            {
                throw new DomainException($"Já existe uma pessoa cadastrada com o CPF {cpfVo.Formatado()}.");
            }

            var telVo = new Telefone(request.Telefone!);
            pessoa = new Pessoa(request.Nome!, cpfVo, request.DtNasc!.Value, telVo, request.Observacao);
            pessoa = await pessoaRepositorio.Adicionar(pessoa);
        }

        var paciente = new Paciente(pessoa);
        var salvo = await pacienteRepositorio.Adicionar(paciente);
        return MapearResponse(salvo);
    }

    public async Task<PacienteResponse> AtualizarAsync(int id, PacienteRequest request)
    {
        var paciente = await pacienteRepositorio.ObterDetalhadoPorId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        if (paciente.Pessoa is not null && !string.IsNullOrWhiteSpace(request.Nome))
        {
            var cpf = !string.IsNullOrWhiteSpace(request.Cpf) ? new Cpf(request.Cpf) : paciente.Pessoa.Cpf;
            var tel = !string.IsNullOrWhiteSpace(request.Telefone) ? new Telefone(request.Telefone) : paciente.Pessoa.Telefone;
            var dtNasc = request.DtNasc ?? paciente.Pessoa.DtNasc;

            if (await pessoaRepositorio.ExisteCpf(cpf.Valor, excluirId: paciente.Pessoa.Id))
            {
                throw new DomainException($"Já existe outra pessoa cadastrada com o CPF {cpf.Formatado()}.");
            }

            paciente.Pessoa.Atualizar(request.Nome, cpf, dtNasc, tel, request.Observacao ?? paciente.Pessoa.Observacao);
            await pessoaRepositorio.Atualizar(paciente.Pessoa);
        }

        var atualizado = await pacienteRepositorio.Atualizar(paciente);
        return MapearResponse(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var paciente = await pacienteRepositorio.ObterId(id)
            ?? throw new DomainException($"Paciente com id {id} não encontrado.");

        await pacienteRepositorio.Remover(paciente);
    }

    public static PacienteResponse MapearResponse(Paciente p) =>
        new(
            p.Id,
            p.IdPessoa,
            p.Pessoa?.Nome ?? string.Empty,
            p.Pessoa?.Cpf?.Formatado() ?? string.Empty,
            p.Pessoa?.DtNasc ?? DateTime.MinValue,
            p.Pessoa?.Telefone?.Formatado() ?? string.Empty,
            p.Pessoa?.Observacao,
            p.DtCriacao
        );
}
