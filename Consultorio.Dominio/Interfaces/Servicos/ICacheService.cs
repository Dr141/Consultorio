namespace Consultorio.Dominio.Interfaces.Servicos;

public interface ICacheService
{
    Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default);
    Task DefinirAsync<T>(string chave, T valor, TimeSpan? tempoExpiracao = null, CancellationToken cancellationToken = default);
    Task RemoverAsync(string chave, CancellationToken cancellationToken = default);
}
