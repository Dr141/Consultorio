using Consultorio.Dominio.Entidades.Base;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IRepositorio<TEntidade> where TEntidade : Entidade
{
    Task<TEntidade?> ObterId(int id);
    Task<List<TEntidade>> ObterTodos();
    Task<TEntidade> Adicionar(TEntidade entidade);
    Task<TEntidade> Atualizar(TEntidade entidade);
    Task Remover(TEntidade entidade);
}
