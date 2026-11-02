using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Controllers.Exame;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class ExameControllerTests
{
    private readonly IExameAppService _exameAppService = Substitute.For<IExameAppService>();
    private readonly ExameController _sut;

    public ExameControllerTests()
    {
        _sut = new ExameController(_exameAppService);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new ExameRequest(1, "Hemograma", null, null);
        var response = new ExameResponse(1, 1, "Maria Silva", "Hemograma", null, null, null, DateTime.UtcNow);

        _exameAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
    }
}
