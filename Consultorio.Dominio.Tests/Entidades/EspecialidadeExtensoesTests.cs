using Consultorio.Dominio.Enumerados;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class EspecialidadeExtensoesTests
{
    [Theory]
    [InlineData(Especialidade.ClinicoGeral, "Clínico Geral")]
    [InlineData(Especialidade.Cardiologista, "Cardiologista")]
    [InlineData(Especialidade.Oftalmologista, "Oftalmologista")]
    public void ObterNome_DeveRetornarNomeLegivel(Especialidade especialidade, string esperado)
    {
        especialidade.ObterNome().Should().Be(esperado);
    }

    [Fact]
    public void ObterNome_TodasEspecialidadesDevemTerNomeSemCamelCase()
    {
        foreach (var especialidade in Enum.GetValues<Especialidade>())
        {
            especialidade.ObterNome().Should().NotBeNullOrWhiteSpace();
        }
    }
}
