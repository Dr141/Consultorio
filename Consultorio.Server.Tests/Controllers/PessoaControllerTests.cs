using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Controllers.Pessoa;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class PessoaControllerTests
{
    private readonly IPessoaAppService _pessoaAppService = Substitute.For<IPessoaAppService>();
    private readonly PessoaController _sut;

    public PessoaControllerTests()
    {
        _sut = new PessoaController(_pessoaAppService);
    }

    [Fact]
    public async Task ObterTodos_DeveRetornar200OkComLista()
    {
        var lista = new List<PessoaResponse>
        {
            new(1, "Juliana Silva", "529.982.247-25", new DateTime(1990, 1, 1), "(11) 98765-4321", null, DateTime.UtcNow)
        };
        _pessoaAppService.ObterTodosAsync().Returns(lista);

        var result = await _sut.ObterTodos();

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().BeEquivalentTo(lista);
    }

    [Fact]
    public async Task ObterPorId_QuandoExiste_DeveRetornar200Ok()
    {
        var pessoa = new PessoaResponse(1, "Juliana Silva", "529.982.247-25", new DateTime(1990, 1, 1), "(11) 98765-4321", null, DateTime.UtcNow);
        _pessoaAppService.ObterPorIdAsync(1).Returns(pessoa);

        var result = await _sut.ObterPorId(1);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().Be(pessoa);
    }

    [Fact]
    public async Task ObterPorId_QuandoNaoExiste_DeveRetornar404NotFound()
    {
        _pessoaAppService.ObterPorIdAsync(99).Returns((PessoaResponse?)null);

        var result = await _sut.ObterPorId(99);

        var notFound = result.Result as NotFoundObjectResult;
        notFound.Should().NotBeNull();
        notFound!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new PessoaRequest("Juliana Silva", "52998224725", new DateTime(1990, 1, 1), "11987654321", null);
        var response = new PessoaResponse(1, "Juliana Silva", "529.982.247-25", new DateTime(1990, 1, 1), "(11) 98765-4321", null, DateTime.UtcNow);

        _pessoaAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
        created.Value.Should().Be(response);
    }
}
