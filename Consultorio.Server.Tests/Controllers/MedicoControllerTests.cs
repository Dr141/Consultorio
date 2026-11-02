using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Enumerados;
using Consultorio.Server.Controllers.Medico;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class MedicoControllerTests
{
    private readonly IMedicoAppService _medicoAppService = Substitute.For<IMedicoAppService>();
    private readonly MedicoController _sut;

    public MedicoControllerTests()
    {
        _sut = new MedicoController(_medicoAppService);
    }

    [Fact]
    public void ObterEspecialidades_DeveRetornarTodasEspecialidades()
    {
        var result = _sut.ObterEspecialidades();

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new MedicoRequest(null, "Dr. Marcelo", "52998224725", new DateTime(1980, 5, 20), "11988887777", null, "12345-SP", [Especialidade.Cardiologista]);
        var response = new MedicoResponse(1, 2, "Dr. Marcelo", "529.982.247-25", new DateTime(1980, 5, 20), "(11) 98888-7777", "12345-SP", [Especialidade.Cardiologista], DateTime.UtcNow);

        _medicoAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
    }
}
