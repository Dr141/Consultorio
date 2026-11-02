using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class PacienteTests
{
    [Fact]
    public void CriarPaciente_ComIdPessoaValido_DeveInstanciarComSucesso()
    {
        var paciente = new Paciente(10);

        paciente.IdPessoa.Should().Be(10);
        paciente.Consultas.Should().BeEmpty();
        paciente.Exames.Should().BeEmpty();
        paciente.Agendas.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CriarPaciente_ComIdInvalido_DeveLancarDomainException(int idInvalido)
    {
        var act = () => new Paciente(idInvalido);

        act.Should().Throw<DomainException>()
            .WithMessage("*inválido*");
    }
}
