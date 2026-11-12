using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IConvenioRepositorio : IRepositorio<Convenio>
{
    Task<bool> ExisteNome(string nome, int? excluirId = null);
}
