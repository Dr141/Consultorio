using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IExameRepositorio : IRepositorio<Exame>
{
    Task<Exame?> ObterDetalhadoPorId(int id);
    Task<List<Exame>> ObterTodosDetalhado();
    Task<List<Exame>> ObterPorPaciente(int idPaciente);
}
