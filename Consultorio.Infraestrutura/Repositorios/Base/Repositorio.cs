using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Interfaces.Base;
using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Infraestrutura.Repositorios.Base;

public abstract class Repositorio<TEntidade> : IRepositorio<TEntidade> where TEntidade : Entidade
{
    private readonly ConsultorioContexto _contexto;
    protected Repositorio(ConsultorioContexto contexto) => _contexto = contexto;

    public Task<TEntidade> Adicionar(TEntidade tentidade)
    {
        throw new NotImplementedException();
    }

    public Task<TEntidade> Atualizar(TEntidade tentidade)
    {
        throw new NotImplementedException();
    }

    public Task<TEntidade> ObterId(int id)
    {
        throw new NotImplementedException();
    }

    public Task<List<TEntidade>> ObterTodos()
    {
        throw new NotImplementedException();
    }

    public Task Remover(TEntidade tentidade)
    {
        throw new NotImplementedException();
    }
}
