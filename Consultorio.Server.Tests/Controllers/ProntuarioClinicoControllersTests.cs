using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Controllers.Cid;
using Consultorio.Server.Controllers.Exame;
using Consultorio.Server.Controllers.Publico;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Consultorio.Server.Tests.Controllers;

public class ExameCicloControllerTests
{
    private readonly IExameAppService _exameAppService = Substitute.For<IExameAppService>();
    private readonly ExameController _sut;

    public ExameCicloControllerTests()
    {
        _sut = new ExameController(_exameAppService)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    [Fact]
    public async Task RegistrarEntrega_SemLaudo_DeveRetornar400()
    {
        _exameAppService.RegistrarEntregaAsync(5).ThrowsAsync(new DomainException("Só é possível entregar exames com laudo emitido."));

        var result = await _sut.RegistrarEntrega(5);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task AdicionarAnexo_SemArquivo_DeveRetornar400SemChamarOServico()
    {
        var result = await _sut.AdicionarAnexo(5, null, CancellationToken.None);

        result.Result.Should().BeOfType<BadRequestObjectResult>();
        await _exameAppService.DidNotReceiveWithAnyArgs().AdicionarAnexoAsync(default, default!, default!, default!);
    }

    [Fact]
    public async Task ObterArquivoAnexo_Inexistente_DeveRetornar404()
    {
        _exameAppService.ObterArquivoAnexoAsync(5, 9).Returns((ArquivoResponse?)null);

        var result = await _sut.ObterArquivoAnexo(5, 9);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ObterArquivoAnexo_DeveDevolverArquivoComNosniff()
    {
        _exameAppService.ObterArquivoAnexoAsync(5, 9).Returns(new ArquivoResponse("laudo.pdf", "application/pdf", [0x25, 0x50, 0x44, 0x46]));

        var result = await _sut.ObterArquivoAnexo(5, 9);

        var arquivo = result.Should().BeOfType<FileContentResult>().Subject;
        arquivo.ContentType.Should().Be("application/pdf");
        _sut.Response.Headers["X-Content-Type-Options"].ToString().Should().Be("nosniff");
        _sut.Response.Headers["Content-Disposition"].ToString().Should().StartWith("inline");
    }
}

public class CidControllerTests
{
    [Fact]
    public void Buscar_PorCodigo_DeveRetornarCidCorrespondente()
    {
        var result = new CidController().Buscar("I10");

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<CidResponse>>()
            .Which.Should().Contain(c => c.Codigo == "I10");
    }
}

public class VerificacaoDocumentoControllerTests
{
    private readonly IDocumentoAppService _documentoAppService = Substitute.For<IDocumentoAppService>();

    [Fact]
    public async Task Verificar_CodigoComHtml_NaoDeveRefletirMarcacao()
    {
        _documentoAppService.VerificarAsync(Arg.Any<string>())
            .Returns(new VerificacaoDocumentoResponse(false, null, null, null, null, null, null, false, "Clínica <Teste>"));

        var resultado = await new VerificacaoDocumentoController(_documentoAppService).Verificar("<script>alert(1)</script>");

        resultado.StatusCode.Should().Be(200);
        resultado.Content.Should().NotContain("<script>");
        resultado.Content.Should().Contain("&lt;Teste&gt;");
        await _documentoAppService.Received(1).VerificarAsync("scriptalert1script");
    }

    [Fact]
    public async Task Verificar_DocumentoCancelado_DeveSinalizarCancelamento()
    {
        _documentoAppService.VerificarAsync("ABCDEFGHJK")
            .Returns(new VerificacaoDocumentoResponse(true, "Atestado médico", DateTime.UtcNow, "Dr. Roberto", "4321/GO", "M. S.", 2, true, "Clínica"));

        var resultado = await new VerificacaoDocumentoController(_documentoAppService).Verificar("ABCDEFGHJK");

        resultado.Content.Should().Contain("CANCELADO");
        resultado.Content.Should().Contain("M. S.");
    }
}
