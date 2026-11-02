using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IPessoaRepositorio : IRepositorio<Pessoa>
{
    Task<Pessoa?> ObterPorCpf(string cpf);
    Task<bool> ExisteCpf(string cpf, int? excluirId = null);
}
