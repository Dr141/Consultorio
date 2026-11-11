using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IBloqueioAgendaRepositorio : IRepositorio<BloqueioAgenda>
{
    /// <summary>Bloqueios que se sobrepõem ao período (do médico informado ou da clínica toda).</summary>
    Task<List<BloqueioAgenda>> ObterNoPeriodo(DateTime inicio, DateTime fim, int? idMedico = null);
    Task<List<BloqueioAgenda>> ObterAPartirDe(DateTime data);
}
