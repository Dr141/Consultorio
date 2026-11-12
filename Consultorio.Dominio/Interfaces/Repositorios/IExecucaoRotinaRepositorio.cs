using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IExecucaoRotinaRepositorio : IRepositorio<ExecucaoRotina>
{
    Task<ExecucaoRotina?> ObterPorNome(string nome);
}
