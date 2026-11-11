using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

/// <summary>
/// Regras da Fase 1: o prontuário não é apagado nem reescrito depois de encerrado.
/// </summary>
public class ProntuarioImutavelTests
{
    [Fact]
    public void ConsultaNova_DeveIniciarAberta()
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");

        consulta.Status.Should().Be(StatusConsulta.Aberta);
        consulta.DtFinalizacao.Should().BeNull();
    }

    [Fact]
    public void Finalizar_DeveBloquearEdicaoDaEvolucao()
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");

        consulta.Finalizar();
        var act = () => consulta.AtualizarObservacao("Texto reescrito");

        consulta.Status.Should().Be(StatusConsulta.Finalizada);
        consulta.DtFinalizacao.Should().NotBeNull();
        act.Should().Throw<DomainException>().WithMessage("*adendo*");
        consulta.Observacao.Should().Be("Evolução inicial");
    }

    [Fact]
    public void AdicionarAdendo_EmConsultaFinalizada_DeveRegistrarAutorETexto()
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");
        consulta.Finalizar();

        var adendo = consulta.AdicionarAdendo("  Resultado do raio-X anexado.  ", "dra.ana@clinica.com");

        consulta.Adendos.Should().ContainSingle();
        adendo.Texto.Should().Be("Resultado do raio-X anexado.");
        adendo.Autor.Should().Be("dra.ana@clinica.com");
    }

    [Fact]
    public void AdicionarAdendo_EmConsultaAberta_DeveOrientarEdicaoDireta()
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");

        var act = () => consulta.AdicionarAdendo("Complemento", "medico@clinica.com");

        act.Should().Throw<DomainException>().WithMessage("*edite a evolução*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("erro")]
    public void Anular_SemMotivoSuficiente_DeveLancarDomainException(string motivo)
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");

        var act = () => consulta.Anular(motivo);

        act.Should().Throw<DomainException>().WithMessage("*motivo*");
        consulta.Status.Should().Be(StatusConsulta.Aberta);
    }

    [Fact]
    public void Anular_ComMotivo_DeveManterRegistroComMotivo()
    {
        var consulta = new Consulta(1, 2, "Evolução inicial");

        consulta.Anular("Registrada no paciente errado");

        consulta.Status.Should().Be(StatusConsulta.Anulada);
        consulta.MotivoAnulacao.Should().Be("Registrada no paciente errado");
        consulta.Observacao.Should().Be("Evolução inicial");
        consulta.Invoking(c => c.Finalizar()).Should().Throw<DomainException>();
    }

    [Fact]
    public void CancelarExame_SemLaudo_DeveRegistrarMotivo()
    {
        var exame = new Exame(1, "Hemograma completo");

        exame.Cancelar("Paciente desistiu do exame");

        exame.Cancelado.Should().BeTrue();
        exame.MotivoCancelamento.Should().Be("Paciente desistiu do exame");
        exame.Invoking(e => e.RegistrarResultado("Normal")).Should().Throw<DomainException>();
    }

    [Fact]
    public void CancelarExame_ComLaudo_DeveLancarDomainException()
    {
        var exame = new Exame(1, "Hemograma completo", resultado: "Sem alterações");

        var act = () => exame.Cancelar("Tentativa de cancelamento");

        act.Should().Throw<DomainException>().WithMessage("*laudo*");
    }

    [Fact]
    public void Medico_VincularUsuario_DeveNormalizarEReconhecerDono()
    {
        var pessoa = new Pessoa("Dra. Ana Souza", new Cpf("52998224725"), new DateTime(1980, 1, 1), new Telefone("62999998888"));
        var medico = new Medico(pessoa, new Crm("12345/GO"), [Especialidade.ClinicoGeral]);

        medico.VincularUsuario("  Dra.Ana@Clinica.com ");

        medico.EmailUsuario.Should().Be("dra.ana@clinica.com");
        medico.PertenceAoUsuario("DRA.ANA@clinica.com").Should().BeTrue();
        medico.PertenceAoUsuario("outro@clinica.com").Should().BeFalse();
    }

    [Fact]
    public void Paciente_InativarEReativar_DeveAlternarSituacao()
    {
        var pessoa = new Pessoa("Carlos Lima", new Cpf("52998224725"), new DateTime(1990, 5, 5), new Telefone("62988887777"));
        var paciente = new Paciente(pessoa);

        paciente.Inativar();
        paciente.Ativo.Should().BeFalse();
        paciente.DtInativacao.Should().NotBeNull();
        paciente.Invoking(p => p.Inativar()).Should().Throw<DomainException>();

        paciente.Reativar();
        paciente.Ativo.Should().BeTrue();
        paciente.DtInativacao.Should().BeNull();
    }
}
