using System.Text.Json;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Middlewares;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Consultorio.Server.Tests.Middlewares;

public class TratamentoErrosMiddlewareTests
{
    private static async Task<(int Status, string Mensagem)> Executar(Exception excecao)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new TratamentoErrosMiddleware(
            _ => throw excecao,
            NullLogger<TratamentoErrosMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var json = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, json.RootElement.GetProperty("message").GetString()!);
    }

    [Fact]
    public async Task DomainException_DeveRetornar400ComMensagemDoDominio()
    {
        var (status, mensagem) = await Executar(new DomainException("CPF informado é inválido."));

        status.Should().Be(StatusCodes.Status400BadRequest);
        mensagem.Should().Be("CPF informado é inválido.");
    }

    [Fact]
    public async Task ExcecaoInesperada_DeveRetornar500SemExporDetalhesTecnicos()
    {
        var (status, mensagem) = await Executar(new InvalidOperationException("NullReference em Repositorio.cs linha 42"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        mensagem.Should().Be(TratamentoErrosMiddleware.MensagemErroInesperado);
    }

    [Fact]
    public async Task UnauthorizedAccess_DeveRetornar401()
    {
        var (status, _) = await Executar(new UnauthorizedAccessException("Sessão expirada"));

        status.Should().Be(StatusCodes.Status401Unauthorized);
    }

    [Fact]
    public async Task SemExcecao_NaoDeveAlterarResposta()
    {
        var context = new DefaultHttpContext();
        var middleware = new TratamentoErrosMiddleware(
            ctx => { ctx.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; },
            NullLogger<TratamentoErrosMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status204NoContent);
    }
}
