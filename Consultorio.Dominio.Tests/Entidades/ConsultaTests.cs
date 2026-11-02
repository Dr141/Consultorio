using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class ConsultaTests
{
    [Fact]
    public void CriarConsulta_ComDadosValidos_DeveInstanciarComSucesso()
    {
        var consulta = new Consulta(idPaciente: 1, idMedico: 2, observacao: "Paciente relata cefaleia frequente.", idAgenda: 3);

        consulta.IdPaciente.Should().Be(1);
        consulta.IdMedico.Should().Be(2);
        consulta.Observacao.Should().Be("Paciente relata cefaleia frequente.");
        consulta.IdAgenda.Should().Be(3);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CriarConsulta_ComObservacaoVazia_DeveLancarDomainException(string observacao)
    {
        var act = () => new Consulta(1, 2, observacao);

        act.Should().Throw<DomainException>()
            .WithMessage("*obrigatória*");
    }

    [Fact]
    public void AtualizarObservacao_ComTextoValido_DeveAtualizar()
    {
        var consulta = new Consulta(1, 2, "Primeira anotação");

        consulta.AtualizarObservacao("Segunda anotação revisada");

        consulta.Observacao.Should().Be("Segunda anotação revisada");
    }
}
