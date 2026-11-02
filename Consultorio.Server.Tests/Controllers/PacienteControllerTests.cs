using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Controllers.Paciente;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class PacienteControllerTests
{
    private readonly IPacienteAppService _pacienteAppService = Substitute.For<IPacienteAppService>();
    private readonly PacienteController _sut;

    public PacienteControllerTests()
    {
        _sut = new PacienteController(_pacienteAppService);
    }

    [Fact]
    public async Task ObterTodos_DeveRetornar200Ok()
    {
        var lista = new List<PacienteResponse>
        {
            new(1, 10, "Lucas Santos", "529.982.247-25", new DateTime(1995, 2, 10), "(11) 98765-4321", null, DateTime.UtcNow)
        };
        _pacienteAppService.ObterTodosAsync().Returns(lista);

        var result = await _sut.ObterTodos();

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new PacienteRequest(10, null, null, null, null, null);
        var response = new PacienteResponse(1, 10, "Lucas Santos", "529.982.247-25", new DateTime(1995, 2, 10), "(11) 98765-4321", null, DateTime.UtcNow);

        _pacienteAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
    }
}
