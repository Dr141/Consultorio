using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Infraestrutura.Cache;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class CacheServiceTests
{
    private readonly RedisCacheService _cacheService;
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisCacheService> _logger;

    public CacheServiceTests()
    {
        var options = Options.Create(new MemoryDistributedCacheOptions());
        _distributedCache = new MemoryDistributedCache(options);
        _logger = Substitute.For<ILogger<RedisCacheService>>();
        _cacheService = new RedisCacheService(_distributedCache, _logger);
    }

    [Fact]
    public async Task DefinirEObterAsync_DeveGravarERecuperarDadosCorretamente()
    {
        // Arrange
        var dados = new List<string> { "Clinico Geral", "Cardiologista" };
        const string chave = "teste:especialidades";

        // Act
        await _cacheService.DefinirAsync(chave, dados, TimeSpan.FromMinutes(5));
        var resultado = await _cacheService.ObterAsync<List<string>>(chave);

        // Assert
        resultado.Should().NotBeNull();
        resultado.Should().BeEquivalentTo(dados);
    }

    [Fact]
    public async Task RemoverAsync_DeveExcluirChaveDoCache()
    {
        // Arrange
        const string chave = "teste:remover";
        await _cacheService.DefinirAsync(chave, "valor");

        // Act
        await _cacheService.RemoverAsync(chave);
        var resultado = await _cacheService.ObterAsync<string>(chave);

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public async Task ObterAsync_QuandoOcorreExcecao_NaoDeveLancarExcecaoERetornarDefault()
    {
        // Arrange
        var brokenCache = Substitute.For<IDistributedCache>();
        brokenCache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<byte[]?>(_ => throw new InvalidOperationException("Redis indisponível"));

        var service = new RedisCacheService(brokenCache, _logger);

        // Act
        var resultado = await service.ObterAsync<string>("chave");

        // Assert
        resultado.Should().BeNull();
    }

    [Fact]
    public async Task MedicoAppService_QuandoCacheExiste_DeveRetornarDoCacheSemConsultarRepositorio()
    {
        // Arrange
        var medicoRepo = Substitute.For<IMedicoRepositorio>();
        var pessoaRepo = Substitute.For<IPessoaRepositorio>();
        var mockCache = Substitute.For<ICacheService>();

        var cachedResponse = new List<MedicoResponse>
        {
            new(1, 1, "Dr. House", "123.456.789-00", new DateTime(1980, 1, 1), "(11) 99999-9999", "12345/SP", new List<Especialidade> { Especialidade.ClinicoGeral }, DateTime.UtcNow)
        };

        mockCache.ObterAsync<List<MedicoResponse>>("medicos:todos")
            .Returns(cachedResponse);

        var appService = new MedicoAppService(medicoRepo, pessoaRepo, mockCache);

        // Act
        var resultado = await appService.ObterTodosAsync();

        // Assert
        resultado.Should().BeEquivalentTo(cachedResponse);
        await medicoRepo.DidNotReceive().ObterTodosDetalhado();
    }
}
