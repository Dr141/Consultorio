using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.ValueObjects;

public class TelefoneTests
{
    [Theory]
    [InlineData("11987654321", "(11) 98765-4321")]
    [InlineData("(11) 98765-4321", "(11) 98765-4321")]
    [InlineData("1133334444", "(11) 3333-4444")]
    public void CriarTelefone_ComValorValido_DeveCriarComSucesso(string entrada, string formatadoEsperado)
    {
        var tel = new Telefone(entrada);

        tel.Formatado().Should().Be(formatadoEsperado);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarTelefone_VazioOuNulo_DeveLancarDomainException(string? valor)
    {
        var act = () => new Telefone(valor!);

        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012")]
    public void CriarTelefone_ComQtdDigitosInvalida_DeveLancarDomainException(string valor)
    {
        var act = () => new Telefone(valor);

        act.Should().Throw<DomainException>()
            .WithMessage("*10 ou 11 dígitos*");
    }
}
