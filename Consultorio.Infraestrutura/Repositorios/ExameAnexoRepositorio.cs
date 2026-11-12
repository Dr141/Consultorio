using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ExameAnexoRepositorio(ConsultorioContexto contexto) : Repositorio<ExameAnexo>(contexto), IExameAnexoRepositorio
{
    public override Task Remover(ExameAnexo entidade) =>
        throw new InvalidOperationException("Anexos de exames fazem parte do prontuário e não podem ser removidos.");

    public async Task<List<ExameAnexo>> ObterResumoPorExame(int idExame)
    {
        // Projeção sem a coluna "Conteudo" para não trafegar os arquivos na listagem.
        var linhas = await _contexto.ExameAnexo
            .Where(a => a.IdExame == idExame)
            .OrderBy(a => a.DtCriacao)
            .Select(a => new { a.Id, a.IdExame, a.NomeArquivo, a.ContentType, a.TamanhoBytes, a.EnviadoPor, a.DtCriacao })
            .ToListAsync();

        return linhas
            .Select(a => ExameAnexo.Resumo(a.Id, a.IdExame, a.NomeArquivo, a.ContentType, a.TamanhoBytes, a.EnviadoPor, a.DtCriacao))
            .ToList();
    }

    public async Task<Dictionary<int, int>> ContarPorExames(IEnumerable<int> idsExames)
    {
        var ids = idsExames.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, int>();

        return await _contexto.ExameAnexo
            .Where(a => ids.Contains(a.IdExame))
            .GroupBy(a => a.IdExame)
            .Select(g => new { g.Key, Total = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Total);
    }
}
