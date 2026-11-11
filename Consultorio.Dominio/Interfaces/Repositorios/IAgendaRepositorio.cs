using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IAgendaRepositorio : IRepositorio<Agenda>
{
    Task<Agenda?> ObterDetalhadoPorId(int id);
    Task<List<Agenda>> ObterTodosDetalhado();
    Task<List<Agenda>> ObterPorMedicoEData(int idMedico, DateTime data);
    Task<List<Agenda>> ObterPorPaciente(int idPaciente);
    Task<bool> ExisteConflitoHorario(int idMedico, DateTime data, TimeSpan hora, int? excluirId = null);
    /// <summary>Horários (livres e ocupados) entre as datas, opcionalmente de um médico.</summary>
    Task<List<Agenda>> ObterPorPeriodo(DateTime inicio, DateTime fim, int? idMedico = null);
    Task<Agenda?> ObterPorToken(Guid token);
    Task AdicionarVarios(IEnumerable<Agenda> agendas);
    Task RemoverVarios(IEnumerable<Agenda> agendas);
}
