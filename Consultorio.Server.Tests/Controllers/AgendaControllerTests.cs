using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Controllers.Agenda;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class AgendaControllerTests
{
    private readonly IAgendaAppService _agendaAppService = Substitute.For<IAgendaAppService>();
    private readonly AgendaController _sut;

    public AgendaControllerTests()
    {
        _sut = new AgendaController(_agendaAppService);
    }

    [Fact]
    public async Task ObterTodos_DeveRetornar200Ok()
    {
        _agendaAppService.ObterTodosAsync().Returns([]);

        var result = await _sut.ObterTodos();

        var okResult = result.Result as OkObjectResult;
        okResult.Should().NotBeNull();
        okResult!.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task Adicionar_ComDadosValidos_DeveRetornar201Created()
    {
        var request = new AgendaRequest(DateTime.UtcNow.Date.AddDays(1), new TimeSpan(10, 0, 0), 1, null, null, null);
        var response = new AgendaResponse(1, request.Data, request.Hora, 1, "Dr. Marcelo", null, null, true, null, null, DateTime.UtcNow);

        _agendaAppService.AdicionarAsync(request).Returns(response);

        var result = await _sut.Adicionar(request);

        var created = result.Result as CreatedAtActionResult;
        created.Should().NotBeNull();
        created!.StatusCode.Should().Be(201);
    }
}
