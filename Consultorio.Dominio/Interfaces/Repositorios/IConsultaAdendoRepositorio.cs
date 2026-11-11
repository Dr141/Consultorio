using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IConsultaAdendoRepositorio : IRepositorio<ConsultaAdendo>
{
    Task<List<ConsultaAdendo>> ObterPorConsulta(int idConsulta);
}
