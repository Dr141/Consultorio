using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface ICobrancaRepositorio : IRepositorio<Cobranca>
{
    Task<Cobranca?> ObterDetalhadoPorId(int id);
    /// <summary>Cobranças criadas no período (datas locais, fim inclusivo), opcionalmente filtradas.</summary>
    Task<List<Cobranca>> ObterPorPeriodo(DateTime inicio, DateTime fim, StatusCobranca? status = null, int? idPaciente = null);
    /// <summary>Cobranças pagas no período (pela data do pagamento).</summary>
    Task<List<Cobranca>> ObterPagasNoPeriodo(DateTime inicioUtc, DateTime fimUtc);
    Task<List<Cobranca>> ObterPendentes();
    Task<List<Cobranca>> ObterPorPaciente(int idPaciente);
    /// <summary>Ids dos atendimentos (agenda) que já têm cobrança não cancelada.</summary>
    Task<HashSet<int>> AgendasComCobranca(IEnumerable<int> idsAgenda);
}
