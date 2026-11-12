using Consultorio.Dominio.Entidades;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class AcessoPortalTests
{
    private static readonly DateTime Agora = new(2027, 3, 10, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Solicitar_DeveGerarCodigoDeSeisDigitosEGuardarSoOHash()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10), "10.0.0.1");

        codigo.Should().MatchRegex("^[0-9]{6}$");
        acesso.CodigoHash.Should().NotContain(codigo);
        acesso.Chave.Should().NotBeEmpty();
        acesso.CodigoExpiraEm.Should().Be(Agora.AddMinutes(10));
    }

    [Fact]
    public void ValidarCodigo_Correto_DeveAbrirSessaoValida()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10));

        acesso.ValidarCodigo(codigo, Agora.AddMinutes(2)).Should().BeTrue();
        var token = acesso.AbrirSessao(Agora.AddMinutes(2), TimeSpan.FromMinutes(30));

        token.Should().HaveLength(43);
        acesso.TokenHash.Should().Be(AcessoPortal.HashToken(token));
        acesso.SessaoValida(Agora.AddMinutes(20)).Should().BeTrue();
        acesso.SessaoValida(Agora.AddMinutes(33)).Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_Expirado_DeveRecusar()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10));

        acesso.ValidarCodigo(codigo, Agora.AddMinutes(11)).Should().BeFalse();
    }

    [Fact]
    public void ValidarCodigo_ErrosSeguidos_DeveBloquearInclusiveOCodigoCerto()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10));
        var errado = codigo == "000000" ? "111111" : "000000";

        for (var i = 0; i < AcessoPortal.MaximoTentativas; i++)
            acesso.ValidarCodigo(errado, Agora).Should().BeFalse();

        acesso.TentativasEsgotadas.Should().BeTrue();
        acesso.ValidarCodigo(codigo, Agora).Should().BeFalse();
    }

    [Fact]
    public void Codigo_JaUtilizado_NaoPodeAbrirOutraSessao()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10));
        acesso.ValidarCodigo(codigo, Agora);
        acesso.AbrirSessao(Agora, TimeSpan.FromMinutes(30));

        acesso.ValidarCodigo(codigo, Agora).Should().BeFalse();
    }

    [Fact]
    public void Encerrar_DeveInvalidarSessao()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora, TimeSpan.FromMinutes(10));
        acesso.ValidarCodigo(codigo, Agora);
        acesso.AbrirSessao(Agora, TimeSpan.FromMinutes(30));

        acesso.Encerrar(Agora.AddMinutes(1));

        acesso.SessaoValida(Agora.AddMinutes(2)).Should().BeFalse();
    }
}
