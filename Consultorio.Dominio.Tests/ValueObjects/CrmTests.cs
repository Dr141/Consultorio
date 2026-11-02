using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.ValueObjects;

public class CrmTests
{
    [Theory]
    [InlineData("12345-SP")]
    [InlineData("CRM/RJ 98765")]
    [InlineData("123456")]
    public void CriarCrm_ComValorValido_DeveCriarComSucesso(string valor)
    {
        var crm = new Crm(valor);

        crm.Valor.Should().Be(valor.Trim());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CriarCrm_VazioOuNulo_DeveLancarDomainException(string? valor)
    {
        var act = () => new Crm(valor!);

        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser vazio*");
    }

    [Fact]
    public void CriarCrm_ComMaisDe20Caracteres_DeveLancarDomainException()
    {
        var act = () => new Crm(new string('A', 21));

        act.Should().Throw<DomainException>()
            .WithMessage("*entre 2 e 20 caracteres*");
    }
}
