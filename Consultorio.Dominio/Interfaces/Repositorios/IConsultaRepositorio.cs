using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IConsultaRepositorio : IRepositorio<Consulta>
{
    Task<Consulta?> ObterDetalhadoPorId(int id);
    Task<List<Consulta>> ObterTodosDetalhado();
    Task<List<Consulta>> ObterPorPaciente(int idPaciente);
    Task<List<Consulta>> ObterPorMedico(int idMedico);
}
