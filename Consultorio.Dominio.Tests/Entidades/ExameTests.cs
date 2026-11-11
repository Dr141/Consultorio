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
        var exame = new Exame(idPaciente: 1, descricao: "Hemograma completo", idAgenda: 2, idMedico: 3);

        exame.IdPaciente.Should().Be(1);
        exame.Descricao.Should().Be("Hemograma completo");
        exame.Resultado.Should().BeNull();
        exame.IdAgenda.Should().Be(2);
        exame.IdMedico.Should().Be(3);
    }

    [Fact]
    public void CriarExame_ComIdMedicoInvalido_DeveLancarDomainException()
    {
        var act = () => new Exame(idPaciente: 1, descricao: "Raio-X de Tórax", idAgenda: null, idMedico: -1);

        act.Should().Throw<DomainException>()
            .WithMessage("*Médico inválido*");
    }

    [Fact]
    public void RegistrarResultado_ComMedicoValido_DeveAtualizarResultadoEMedico()
    {
        var exame = new Exame(1, "Hemograma completo");

        exame.RegistrarResultado("Valores dentro da normalidade.", idMedico: 4);

        exame.Resultado.Should().Be("Valores dentro da normalidade.");
        exame.IdMedico.Should().Be(4);
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
