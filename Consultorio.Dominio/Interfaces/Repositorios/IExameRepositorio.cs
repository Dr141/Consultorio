using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IExameRepositorio : IRepositorio<Exame>
{
    Task<Exame?> ObterDetalhadoPorId(int id);
    Task<List<Exame>> ObterTodosDetalhado();
    /// <summary>Exames não cancelados ainda sem entrega (solicitados, realizados ou laudados).</summary>
    Task<List<Exame>> ObterEmAndamento();
    Task<List<Exame>> ObterPorPaciente(int idPaciente);
    Task<List<Exame>> ObterPorPacienteFiltrado(int idPaciente, int? idMedico = null, DateTime? dataInicio = null, DateTime? dataFim = null);
}
