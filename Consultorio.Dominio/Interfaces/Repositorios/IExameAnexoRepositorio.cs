using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IExameAnexoRepositorio : IRepositorio<ExameAnexo>
{
    /// <summary>Anexos do exame sem o conteúdo binário.</summary>
    Task<List<ExameAnexo>> ObterResumoPorExame(int idExame);
    Task<Dictionary<int, int>> ContarPorExames(IEnumerable<int> idsExames);
}
