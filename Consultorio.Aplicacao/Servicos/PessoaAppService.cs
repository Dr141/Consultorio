using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

public class PessoaAppService(
    IPessoaRepositorio pessoaRepositorio,
    ICacheService? cacheService = null) : IPessoaAppService
{
    private const string CacheKeyTodos = "pessoas:todos";

    public async Task<List<PessoaResponse>> ObterTodosAsync()
    {
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<List<PessoaResponse>>(CacheKeyTodos);
            if (emCache is not null)
                return emCache;
        }

        var pessoas = await pessoaRepositorio.ObterTodos();
        var response = pessoas.Select(MapearResponse).ToList();

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(CacheKeyTodos, response);
        }

        return response;
    }

    public async Task<PessoaResponse?> ObterPorIdAsync(int id)
    {
        string cacheKey = $"pessoas:{id}";
        if (cacheService is not null)
        {
            var emCache = await cacheService.ObterAsync<PessoaResponse>(cacheKey);
            if (emCache is not null)
                return emCache;
        }

        var pessoa = await pessoaRepositorio.ObterId(id);
        if (pessoa is null) return null;

        var response = MapearResponse(pessoa);

        if (cacheService is not null)
        {
            await cacheService.DefinirAsync(cacheKey, response);
        }

        return response;
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

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"pessoas:{salvo.Id}");
        }

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

        if (cacheService is not null)
        {
            await cacheService.RemoverAsync(CacheKeyTodos);
            await cacheService.RemoverAsync($"pessoas:{id}");
        }

        return MapearResponse(atualizado);
    }

    public async Task RemoverAsync(int id)
    {
        _ = await pessoaRepositorio.ObterId(id)
            ?? throw new DomainException($"Pessoa com id {id} não encontrada.");

        // Excluir a pessoa apagaria em cascata o paciente/médico e todo o prontuário vinculado.
        throw new DomainException("Cadastros de pessoas não são excluídos. Inative o paciente ou o médico correspondente.");
    }

    public static PessoaResponse MapearResponse(Pessoa p) =>
        new(p.Id, p.Nome, p.Cpf?.Formatado() ?? string.Empty, p.DtNasc, p.Telefone?.Formatado() ?? string.Empty, p.Observacao, p.DtCriacao);
}
