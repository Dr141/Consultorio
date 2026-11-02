using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class ExameTests
{
    [Fact]
    public void CriarExame_ComDadosValidos_DeveInstanciarComSucesso()
    {
        var exame = new Exame(idPaciente: 1, descricao: "Hemograma completo", idAgenda: 2);

        exame.IdPaciente.Should().Be(1);
        exame.Descricao.Should().Be("Hemograma completo");
        exame.Resultado.Should().BeNull();
        exame.IdAgenda.Should().Be(2);
    }

    [Fact]
    public void RegistrarResultado_ComTextoValido_DeveDefinirResultado()
    {
        var exame = new Exame(1, "Hemograma completo");

        exame.RegistrarResultado("Valores dentro da normalidade.");

        exame.Resultado.Should().Be("Valores dentro da normalidade.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RegistrarResultado_VazioOuNulo_DeveLancarDomainException(string? resultado)
    {
        var exame = new Exame(1, "Hemograma completo");

        var act = () => exame.RegistrarResultado(resultado!);

        act.Should().Throw<DomainException>()
            .WithMessage("*não pode ser vazio*");
    }
}
