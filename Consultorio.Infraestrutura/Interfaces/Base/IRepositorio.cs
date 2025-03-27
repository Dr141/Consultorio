using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Infraestrutura.Interfaces.Base;

public interface IRepositorio<TEntidade> where TEntidade : Entidade
{
    public Task<TEntidade?> ObterId(int id);
    public Task<List<TEntidade>> ObterTodos();
    public Task<TEntidade> Atualizar(TEntidade entidade);
    public Task<TEntidade> Adicionar(TEntidade entidade);
    public Task Remover(TEntidade entidade);
}
