using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.ValueObjects;

public class CpfTests
{
    [Theory]
    [InlineData("52998224725")]
    [InlineData("529.982.247-25")]
    public void CriarCpf_ComValorValido_DeveCriarComSucesso(string valor)
    {
        var cpf = new Cpf(valor);

        cpf.Valor.Should().Be("52998224725");
        cpf.Formatado().Should().Be("529.982.247-25");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarCpf_VazioOuNulo_DeveLancarDomainException(string? valor)
    {
        var act = () => new Cpf(valor!);

        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("123456789012")]
    public void CriarCpf_ComTamanhoInvalido_DeveLancarDomainException(string valor)
    {
        var act = () => new Cpf(valor);

        act.Should().Throw<DomainException>()
            .WithMessage("*11 dígitos*");
    }

    [Theory]
    [InlineData("11111111111")]
    [InlineData("00000000000")]
    [InlineData("12345678901")]
    public void CriarCpf_Invalido_DeveLancarDomainException(string valor)
    {
        var act = () => new Cpf(valor);

        act.Should().Throw<DomainException>()
            .WithMessage("*inválido*");
    }
}
