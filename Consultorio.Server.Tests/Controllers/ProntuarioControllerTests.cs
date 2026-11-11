using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Controllers.Prontuario;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class ProntuarioControllerTests
{
    private readonly IProntuarioAppService _prontuarioAppService = Substitute.For<IProntuarioAppService>();
    private readonly ProntuarioController _sut;

    public ProntuarioControllerTests()
    {
        _sut = new ProntuarioController(_prontuarioAppService);
    }

    [Fact]
    public async Task ObterProntuario_ComPacienteExistente_DeveRetornar200Ok()
    {
        var pacienteResponse = new PacienteResponse(1, 10, "Lucas Santos", "529.982.247-25", new DateTime(1995, 2, 10), "(11) 98765-4321", null, DateTime.UtcNow);
        var resumo = new ProntuarioResumoResponse(2, 3, 2, 1);
        var prontuarioResponse = new ProntuarioResponse(
            "OF-2026-0001-1234",
            DateTime.UtcNow,
            pacienteResponse,
            null,
            null,
            null,
            resumo,
            [],
            []
        );

        _prontuarioAppService.GerarProntuarioAsync(Arg.Any<ProntuarioFiltroRequest>())
            .Returns(prontuarioResponse);

        var result = await _sut.ObterProntuario(1);

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
        var retorno = okResult.Value as ProntuarioResponse;
        retorno.Should().NotBeNull();
        retorno!.NumeroOficio.Should().Be("OF-2026-0001-1234");
    }

    [Fact]
    public async Task ObterProntuario_QuandoPacienteNaoEncontrado_DeveRetornar404NotFound()
    {
        _prontuarioAppService.GerarProntuarioAsync(Arg.Any<ProntuarioFiltroRequest>())
            .ThrowsAsync(new DomainException("Paciente com id 99 não encontrado."));

        var result = await _sut.ObterProntuario(99);

        var notFoundResult = result.Result as NotFoundObjectResult;
        notFoundResult.Should().NotBeNull();
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task ObterProntuario_QuandoDatasInvalidas_DeveRetornar400BadRequest()
    {
        _prontuarioAppService.GerarProntuarioAsync(Arg.Any<ProntuarioFiltroRequest>())
            .ThrowsAsync(new DomainException("A data inicial não pode ser maior que a data final."));

        var result = await _sut.ObterProntuario(1, dataInicio: DateTime.UtcNow.AddDays(5), dataFim: DateTime.UtcNow);

        var badRequestResult = result.Result as BadRequestObjectResult;
        badRequestResult.Should().NotBeNull();
        badRequestResult!.StatusCode.Should().Be(400);
    }
}
