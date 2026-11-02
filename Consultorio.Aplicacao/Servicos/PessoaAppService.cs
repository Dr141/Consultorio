using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

public class PessoaAppService(IPessoaRepositorio pessoaRepositorio) : IPessoaAppService
{
    public async Task<List<PessoaResponse>> ObterTodosAsync()
    {
        var pessoas = await pessoaRepositorio.ObterTodos();
        return pessoas.Select(MapearResponse).ToList();
    }

    public async Task<PessoaResponse?> ObterPorIdAsync(int id)
    {
        var pessoa = await pessoaRepositorio.ObterId(id);
        return pessoa is null ? null : MapearResponse(pessoa);
    }

    public async Task<PessoaResponse> AdicionarAsync(PessoaRequest request)
    {
        var cpfVo = new Cpf(request.Cpf);
        if (await pessoaRepositorio.ExisteCpf(cpfVo.Valor))
        {
            throw new DomainException($"Já existe uma pessoa cadastrada com o CPF {cpfVo.Formatado()}.");
        }

        var telefoneVo = new Telefone(request.Telefone);
        var pessoa = new Pessoa(request.Nome, cpfVo, request.DtNasc, telefoneVo, request.Observacao);

        var salvo = await pessoaRepositorio.Adicionar(pessoa);
        return MapearResponse(salvo);
    }

    public async Task<PessoaResponse> AtualizarAsync(int id, PessoaRequest request)
    {
        var pessoa = await pessoaRepositorio.ObterId(id)
            ?? throw new DomainException($"Pessoa com id {id} não encontrada.");

        var cpfVo = new Cpf(request.Cpf);
        if (await pessoaRepositorio.ExisteCpf(cpfVo.Valor, excluirId: id))
        {
            throw new DomainException($"Já existe outra pessoa cadastrada com o CPF {cpfVo.Formatado()}.");
        }

        var telefoneVo = new Telefone(request.Telefone);
        pessoa.Atualizar(request.Nome, cpfVo, request.DtNasc, telefoneVo, request.Observacao);

        var atualizado = await pessoaRepositorio.Atualizar(pessoa);
        return MapearResponse(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        var pessoa = await pessoaRepositorio.ObterId(id)
            ?? throw new DomainException($"Pessoa com id {id} não encontrada.");

        await pessoaRepositorio.Remover(pessoa);
    }

    public static PessoaResponse MapearResponse(Pessoa p) =>
        new(p.Id, p.Nome, p.Cpf?.Formatado() ?? string.Empty, p.DtNasc, p.Telefone?.Formatado() ?? string.Empty, p.Observacao, p.DtCriacao);
}
