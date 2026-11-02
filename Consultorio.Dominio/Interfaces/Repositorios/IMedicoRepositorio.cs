using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IMedicoRepositorio : IRepositorio<Medico>
{
    Task<Medico?> ObterDetalhadoPorId(int id);
    Task<List<Medico>> ObterTodosDetalhado();
    Task<Medico?> ObterPorCrm(string crm);
    Task<bool> ExisteCrm(string crm, int? excluirId = null);
    Task<Medico?> ObterPorIdPessoa(int idPessoa);
}
