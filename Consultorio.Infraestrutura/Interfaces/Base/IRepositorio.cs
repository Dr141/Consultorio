using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Infraestrutura.Interfaces.Base;

public interface IRepositorio<TEntidade> where TEntidade : Entidade
{
    public Task<TEntidade> ObterId(int id);
    public Task<List<TEntidade>> ObterTodos();
    public Task<TEntidade> Atualizar(TEntidade tentidade);
    public Task<TEntidade> Adicionar(TEntidade tentidade);
    public Task Remover(TEntidade tentidade);
}
