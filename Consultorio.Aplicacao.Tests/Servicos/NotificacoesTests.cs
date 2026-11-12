using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Interfaces.Servicos;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class NotificacoesTests
{
    private static readonly MensagemNotificacao Mensagem = new("Maria", "maria@exemplo.com", "62987654321", "Assunto", "Texto", null);

    private static INotificador Canal(bool podeEnviar, bool? resultado = true, Exception? erro = null)
    {
        var canal = Substitute.For<INotificador>();
        canal.PodeEnviar(Arg.Any<MensagemNotificacao>()).Returns(podeEnviar);
        if (erro is not null) canal.EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>()).Throws(erro);
        else canal.EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>()).Returns(resultado ?? false);
        return canal;
    }

    [Fact]
    public async Task SemCanalDisponivel_DeveRetornarNulo()
    {
        (await Notificacoes.EnviarAsync([Canal(false)], Mensagem, default)).Should().BeNull();
    }

    [Fact]
    public async Task PrimeiroCanalEnvia_NaoDeveUsarOsSeguintes()
    {
        var whatsapp = Canal(true);
        var email = Canal(true);

        (await Notificacoes.EnviarAsync([whatsapp, email], Mensagem, default)).Should().BeTrue();
        await email.DidNotReceive().EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PrimeiroCanalFalha_DeveTentarOProximo()
    {
        var whatsapp = Canal(true, resultado: false);
        var email = Canal(true);

        (await Notificacoes.EnviarAsync([whatsapp, email], Mensagem, default)).Should().BeTrue();
        await email.Received(1).EnviarAsync(Mensagem, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CanalComExcecao_DeveTentarOProximo()
    {
        var whatsapp = Canal(true, erro: new HttpRequestException("fora do ar"));
        var email = Canal(true);

        (await Notificacoes.EnviarAsync([whatsapp, email], Mensagem, default)).Should().BeTrue();
    }

    [Fact]
    public async Task TodosFalham_DeveRetornarFalse()
    {
        (await Notificacoes.EnviarAsync([Canal(true, false), Canal(false), Canal(true, false)], Mensagem, default))
            .Should().BeFalse();
    }
}
