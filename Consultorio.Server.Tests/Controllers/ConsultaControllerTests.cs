using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Controllers.Consulta;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class ConsultaControllerTests
{
    private readonly IConsultaAppService _consultaAppService = Substitute.For<IConsultaAppService>();
    private readonly ConsultaController _sut;

    public ConsultaControllerTests()
    {
        _sut = new ConsultaController(_consultaAppService);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new ConsultaRequest(1, 2, "Paciente com sintomas de gripe.", null);
        var response = new ConsultaResponse(1, 1, "Maria Silva", 2, "Dr. Marcelo", "Paciente com sintomas de gripe.", null, null, DateTime.UtcNow);

        _consultaAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
    }
}
