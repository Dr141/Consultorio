using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class ConvenioAppService(
    IConvenioRepositorio convenioRepositorio,
    ITabelaPrecoRepositorio tabelaPrecoRepositorio) : IConvenioAppService
{
    public async Task<List<ConvenioResponse>> ObterTodosAsync(bool incluirInativos = false)
    {
        var convenios = await convenioRepositorio.ObterTodos();
        return convenios
            .Where(c => incluirInativos || c.Ativo)
            .OrderBy(c => c.Nome, StringComparer.Create(Notificacoes.PtBr, true))
            .Select(Mapear)
            .ToList();
    }

    public async Task<ConvenioResponse?> ObterPorIdAsync(int id)
    {
        var convenio = await convenioRepositorio.ObterId(id);
        return convenio is null ? null : Mapear(convenio);
    }

    public async Task<ConvenioResponse> AdicionarAsync(ConvenioRequest request)
    {
        DomainException.When(await convenioRepositorio.ExisteNome(request.Nome), $"Já existe um convênio chamado \"{request.Nome.Trim()}\".");
        var convenio = new Convenio(request.Nome, request.RegistroAns, request.Telefone, request.Observacao);
        return Mapear(await convenioRepositorio.Adicionar(convenio));
    }

    public async Task<ConvenioResponse> AtualizarAsync(int id, ConvenioRequest request)
    {
        var convenio = await Obter(id);
        DomainException.When(await convenioRepositorio.ExisteNome(request.Nome, id), $"Já existe um convênio chamado \"{request.Nome.Trim()}\".");
        convenio.Atualizar(request.Nome, request.RegistroAns, request.Telefone, request.Observacao);
        return Mapear(await convenioRepositorio.Atualizar(convenio));
    }

    public async Task<ConvenioResponse> InativarAsync(int id)
    {
        var convenio = await Obter(id);
        convenio.Inativar();
        return Mapear(await convenioRepositorio.Atualizar(convenio));
    }

    public async Task<ConvenioResponse> ReativarAsync(int id)
    {
        var convenio = await Obter(id);
        convenio.Reativar();
        return Mapear(await convenioRepositorio.Atualizar(convenio));
    }

    public async Task<List<TabelaPrecoResponse>> ObterPrecosAsync()
    {
        var precos = await tabelaPrecoRepositorio.ObterTodosDetalhado();
        return precos.Select(MapearPreco).ToList();
    }

    public async Task<TabelaPrecoResponse> DefinirPrecoAsync(TabelaPrecoRequest request)
    {
        if (request.IdConvenio.HasValue)
        {
            var convenio = await convenioRepositorio.ObterId(request.IdConvenio.Value);
            DomainException.When(convenio is null, "Convênio não encontrado.");
        }

        var existente = await tabelaPrecoRepositorio.ObterPor(request.Tipo, request.IdConvenio);
        TabelaPreco salvo;
        if (existente is null)
        {
            salvo = await tabelaPrecoRepositorio.Adicionar(new TabelaPreco(request.Tipo, request.IdConvenio, request.Valor));
        }
        else
        {
            existente.DefinirValor(request.Valor);
            salvo = await tabelaPrecoRepositorio.Atualizar(existente);
        }

        var atualizado = (await tabelaPrecoRepositorio.ObterTodosDetalhado()).FirstOrDefault(t => t.Id == salvo.Id) ?? salvo;
        return MapearPreco(atualizado);
    }

    public async Task RemoverPrecoAsync(int id)
    {
        var preco = await tabelaPrecoRepositorio.ObterId(id)
            ?? throw new DomainException($"Preço com id {id} não encontrado.");
        await tabelaPrecoRepositorio.Remover(preco);
    }

    private async Task<Convenio> Obter(int id) =>
        await convenioRepositorio.ObterId(id) ?? throw new DomainException($"Convênio com id {id} não encontrado.");

    public static ConvenioResponse Mapear(Convenio c) =>
        new(c.Id, c.Nome, c.RegistroAns, c.Telefone, c.Observacao, c.Ativo, c.DtCriacao);

    public static TabelaPrecoResponse MapearPreco(TabelaPreco t) =>
        new(t.Id, t.Tipo.ToString(), t.Tipo.ObterNome(), t.IdConvenio, t.Convenio?.Nome, t.Valor, t.DtAtualizacao);
}
