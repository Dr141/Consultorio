using System.Text.Json;
using Consultorio.Dominio.Interfaces.Servicos;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infraestrutura.Cache;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public RedisCacheService(IDistributedCache cache, ILogger<RedisCacheService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T?> ObterAsync<T>(string chave, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = await _cache.GetStringAsync(chave, cancellationToken);
            if (string.IsNullOrEmpty(json))
                return default;

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao obter cache do Redis para a chave {Chave}. Prosseguindo sem cache.", chave);
            return default;
        }
    }

    public async Task DefinirAsync<T>(string chave, T valor, TimeSpan? tempoExpiracao = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(valor, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = tempoExpiracao ?? TimeSpan.FromMinutes(30)
            };

            await _cache.SetStringAsync(chave, json, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao gravar cache no Redis para a chave {Chave}. Prosseguindo sem cache.", chave);
        }
    }

    public async Task RemoverAsync(string chave, CancellationToken cancellationToken = default)
    {
        try
        {
            await _cache.RemoveAsync(chave, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Falha ao remover cache no Redis para a chave {Chave}.", chave);
        }
    }
}
