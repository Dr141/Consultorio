using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Interfaces.Base;
using Consultorio.Modelo.Entidades.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios.Base;
/// <summary>
/// Classe base para os repositórios
/// </summary>
/// <typeparam name="TEntidade"></typeparam>
/// <param name="contexto"></param>
public class Repositorio<TEntidade>(ConsultorioContexto _contexto) : IRepositorio<TEntidade> where TEntidade : Entidade
{
    /// <summary>
    /// Adiciona uma entidade
    /// </summary>
    /// <param name="entidade"></param>
    /// <returns>Retorna a <see cref="TEntidade"/></returns>
    public async Task<TEntidade> Adicionar(TEntidade entidade)
    {
        _contexto.Set<TEntidade>().Add(entidade);
        await _contexto.SaveChangesAsync();
        return entidade;
    }

    /// <summary>
    /// Atualiza uma entidade
    /// </summary>
    /// <param name="entidade"></param>
    /// <returns>Retorna a <see cref="TEntidade"/></returns>
    public Task<TEntidade> Atualizar(TEntidade entidade)
    {
        _contexto.Set<TEntidade>().Update(entidade);
        _contexto.SaveChanges();
        return Task.FromResult(entidade);
    }

    /// <summary>
    /// Obtem uma entidade pelo id
    /// </summary>
    /// <param name="id"></param>
    /// <returns>Retorna a <see cref="TEntidade"/></returns>
    public async Task<TEntidade?> ObterId(int id)
        => await _contexto.Set<TEntidade>().FindAsync(id);

    /// <summary>
    /// Obtem todas as entidades
    /// </summary>
    /// <returns>Retorna uma <see cref="List{TEntidade}"/></returns>
    public async Task<List<TEntidade>> ObterTodos()
        => await _contexto.Set<TEntidade>().AsNoTracking().ToListAsync();

    /// <summary>
    /// Remove uma entidade
    /// </summary>
    /// <param name="entidade"></param>
    /// <returns>Retorna uma <see cref="Task.CompletedTask"/></returns>
    public Task Remover(TEntidade entidade)
    {
        _contexto.Set<TEntidade>().Remove(entidade);
        _contexto.SaveChangesAsync();
        return Task.CompletedTask;
    }
}
