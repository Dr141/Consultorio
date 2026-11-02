using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios.Base;

public class Repositorio<TEntidade>(ConsultorioContexto contexto) : IRepositorio<TEntidade> where TEntidade : Entidade
{
    protected readonly ConsultorioContexto _contexto = contexto;

    public virtual async Task<TEntidade> Adicionar(TEntidade entidade)
    {
        await _contexto.Set<TEntidade>().AddAsync(entidade);
        await _contexto.SaveChangesAsync();
        return entidade;
    }

    public virtual async Task<TEntidade> Atualizar(TEntidade entidade)
    {
        _contexto.Set<TEntidade>().Update(entidade);
        await _contexto.SaveChangesAsync();
        return entidade;
    }

    public virtual async Task<TEntidade?> ObterId(int id)
    {
        return await _contexto.Set<TEntidade>().FindAsync(id);
    }

    public virtual async Task<List<TEntidade>> ObterTodos()
    {
        return await _contexto.Set<TEntidade>().AsNoTracking().ToListAsync();
    }

    public virtual async Task Remover(TEntidade entidade)
    {
        _contexto.Set<TEntidade>().Remove(entidade);
        await _contexto.SaveChangesAsync();
    }
}
