using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class MedicoTests
{
    private readonly Crm _crmValido = new("12345-SP");

    [Fact]
    public void CriarMedico_ComDadosValidos_DeveInstanciarComSucesso()
    {
        var medico = new Medico(5, _crmValido, [Especialidade.Cardiologista, Especialidade.ClinicoGeral]);

        medico.IdPessoa.Should().Be(5);
        medico.Crm.Valor.Should().Be("12345-SP");
        medico.Especialidades.Should().HaveCount(2);
        medico.Especialidades.Should().Contain(Especialidade.Cardiologista);
    }

    [Fact]
    public void CriarMedico_SemEspecialidades_DeveLancarDomainException()
    {
        var act = () => new Medico(5, _crmValido, []);

        act.Should().Throw<DomainException>()
            .WithMessage("*ao menos uma especialidade*");
    }

    [Fact]
    public void AtualizarMedico_ComNovasEspecialidades_DeveAtualizar()
    {
        var medico = new Medico(5, _crmValido, [Especialidade.ClinicoGeral]);
        var novoCrm = new Crm("67890-SP");

        medico.Atualizar(novoCrm, [Especialidade.Ortopedista]);

        medico.Crm.Valor.Should().Be("67890-SP");
        medico.Especialidades.Should().ContainSingle().Which.Should().Be(Especialidade.Ortopedista);
    }

    [Fact]
    public void RemoverEspecialidade_QuandoPossuirApenasUma_DeveLancarDomainException()
    {
        var medico = new Medico(5, _crmValido, [Especialidade.ClinicoGeral]);

        var act = () => medico.RemoverEspecialidade(Especialidade.ClinicoGeral);

        act.Should().Throw<DomainException>()
            .WithMessage("*sem nenhuma especialidade*");
    }
}
