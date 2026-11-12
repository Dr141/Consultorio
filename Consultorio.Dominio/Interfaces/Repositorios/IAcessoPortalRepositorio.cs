using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IAcessoPortalRepositorio : IRepositorio<AcessoPortal>
{
    Task<AcessoPortal?> ObterPorChave(Guid chave);
    Task<AcessoPortal?> ObterPorTokenHash(string tokenHash);
    /// <summary>Quantas solicitações de código o paciente fez desde o instante informado.</summary>
    Task<int> ContarSolicitacoes(int idPaciente, DateTime desdeUtc);
}
