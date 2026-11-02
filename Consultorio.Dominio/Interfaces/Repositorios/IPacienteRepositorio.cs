using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IPacienteRepositorio : IRepositorio<Paciente>
{
    Task<Paciente?> ObterDetalhadoPorId(int id);
    Task<List<Paciente>> ObterTodosDetalhado();
    Task<Paciente?> ObterPorIdPessoa(int idPessoa);
}
