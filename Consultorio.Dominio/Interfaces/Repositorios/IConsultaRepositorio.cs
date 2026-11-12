using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IConsultaRepositorio : IRepositorio<Consulta>
{
    Task<Consulta?> ObterDetalhadoPorId(int id);
    Task<List<Consulta>> ObterTodosDetalhado();
    /// <summary>Consultas não anuladas com retorno previsto no intervalo (datas, fim inclusivo).</summary>
    Task<List<Consulta>> ObterComRetornoEntre(DateTime inicio, DateTime fim);
    /// <summary>Consultas registradas no período (datas locais, fim inclusivo).</summary>
    Task<List<Consulta>> ObterPorPeriodo(DateTime inicio, DateTime fim);
    Task<List<Consulta>> ObterPorPaciente(int idPaciente);
    Task<List<Consulta>> ObterPorMedico(int idMedico);
    Task<List<Consulta>> ObterPorPacienteFiltrado(int idPaciente, int? idMedico = null, DateTime? dataInicio = null, DateTime? dataFim = null);
}
