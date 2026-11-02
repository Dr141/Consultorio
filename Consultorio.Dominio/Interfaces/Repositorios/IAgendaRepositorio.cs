using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IAgendaRepositorio : IRepositorio<Agenda>
{
    Task<Agenda?> ObterDetalhadoPorId(int id);
    Task<List<Agenda>> ObterTodosDetalhado();
    Task<List<Agenda>> ObterPorMedicoEData(int idMedico, DateTime data);
    Task<List<Agenda>> ObterPorPaciente(int idPaciente);
    Task<bool> ExisteConflitoHorario(int idMedico, DateTime data, TimeSpan hora, int? excluirId = null);
}
