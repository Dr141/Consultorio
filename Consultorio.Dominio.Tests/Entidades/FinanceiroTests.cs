using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class ConvenioTests
{
    [Fact]
    public void Criar_DeveNormalizarCampos()
    {
        var convenio = new Convenio("  Unimed  ", "312.345", "  ", null);

        convenio.Nome.Should().Be("Unimed");
        convenio.RegistroAns.Should().Be("312345");
        convenio.Telefone.Should().BeNull();
        convenio.Ativo.Should().BeTrue();
    }

    [Theory]
    [InlineData("A", null)]
    [InlineData("Unimed", "12345")]
    public void Criar_ComDadosInvalidos_DeveLancarDomainException(string nome, string? ans)
    {
        var act = () => new Convenio(nome, ans);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void InativarEReativar_DeveAlternarSituacao()
    {
        var convenio = new Convenio("Bradesco Saúde");

        convenio.Inativar();
        convenio.Ativo.Should().BeFalse();
        convenio.Invoking(c => c.Inativar()).Should().Throw<DomainException>();

        convenio.Reativar();
        convenio.Ativo.Should().BeTrue();
    }
}

public class TabelaPrecoTests
{
    [Fact]
    public void DefinirValor_DeveArredondarEmDuasCasas()
    {
        var preco = new TabelaPreco(TipoAtendimento.Consulta, null, 199.999m);

        preco.Valor.Should().Be(200.00m);
        preco.DtAtualizacao.Should().NotBeNull();
    }

    [Fact]
    public void DefinirValor_Negativo_DeveLancarDomainException()
    {
        var act = () => new TabelaPreco(TipoAtendimento.Retorno, 1, -1m);

        act.Should().Throw<DomainException>().WithMessage("*negativo*");
    }
}

public class CobrancaTests
{
    private static Cobranca Nova(decimal valor = 200m, int? idConvenio = null) =>
        new(1, "Consulta em 10/11/2026", valor, new DateTime(2026, 11, 10), "recepcao@clinica.com", idConvenio: idConvenio);

    [Fact]
    public void Criar_DeveNascerPendenteComVencimentoSomenteData()
    {
        var cobranca = Nova();

        cobranca.Status.Should().Be(StatusCobranca.Pendente);
        cobranca.ValorFinal.Should().Be(200m);
        cobranca.DtVencimento.Kind.Should().Be(DateTimeKind.Utc);
        cobranca.DtVencimento.TimeOfDay.Should().Be(TimeSpan.Zero);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(100001)]
    public void Criar_ComValorInvalido_DeveLancarDomainException(int valor)
    {
        var act = () => Nova(valor);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RegistrarPagamento_ComDesconto_DeveQuitarPeloValorFinal()
    {
        var cobranca = Nova();

        cobranca.AplicarDesconto(20m);
        cobranca.RegistrarPagamento(FormaPagamento.Pix, "recepcao@clinica.com");

        cobranca.Status.Should().Be(StatusCobranca.Paga);
        cobranca.ValorFinal.Should().Be(180m);
        cobranca.FormaPagamento.Should().Be(FormaPagamento.Pix);
        cobranca.DtPagamento.Should().NotBeNull();
        cobranca.RecebidoPor.Should().Be("recepcao@clinica.com");
    }

    [Fact]
    public void AplicarDesconto_IgualAoValor_DeveLancarDomainException()
    {
        var act = () => Nova(100m).AplicarDesconto(100m);

        act.Should().Throw<DomainException>().WithMessage("*menor que o valor*");
    }

    [Fact]
    public void PagarPeloConvenio_CobrancaParticular_DeveLancarDomainException()
    {
        var act = () => Nova().RegistrarPagamento(FormaPagamento.Convenio, "x");

        act.Should().Throw<DomainException>().WithMessage("*convênio*");
    }

    [Fact]
    public void PagarPeloConvenio_CobrancaDeConvenio_DeveSerAceito()
    {
        var cobranca = Nova(idConvenio: 3);

        cobranca.RegistrarPagamento(FormaPagamento.Convenio, "financeiro@clinica.com");

        cobranca.Status.Should().Be(StatusCobranca.Paga);
    }

    [Fact]
    public void Pagar_DuasVezes_DeveLancarDomainException()
    {
        var cobranca = Nova();
        cobranca.RegistrarPagamento(FormaPagamento.Dinheiro, "x");

        var act = () => cobranca.RegistrarPagamento(FormaPagamento.Pix, "x");

        act.Should().Throw<DomainException>().WithMessage("*pendentes*");
    }

    [Fact]
    public void Cancelar_Pendente_DeveGuardarMotivo_EPagaNaoPodeSerCancelada()
    {
        var pendente = Nova();
        pendente.Cancelar("Lançada em duplicidade");
        pendente.Status.Should().Be(StatusCobranca.Cancelada);
        pendente.MotivoCancelamento.Should().Be("Lançada em duplicidade");

        var paga = Nova();
        paga.RegistrarPagamento(FormaPagamento.Pix, "x");
        paga.Invoking(c => c.Cancelar("Tentativa de cancelar")).Should().Throw<DomainException>();
    }

    [Fact]
    public void Estornar_SoCobrancaPaga()
    {
        var pendente = Nova();
        pendente.Invoking(c => c.Estornar("Paciente desistiu")).Should().Throw<DomainException>().WithMessage("*pagas*");

        var paga = Nova();
        paga.RegistrarPagamento(FormaPagamento.CartaoCredito, "x");
        paga.Estornar("Cobrança indevida");
        paga.Status.Should().Be(StatusCobranca.Estornada);
    }

    [Fact]
    public void Vencida_DependeDaDataEDaSituacao()
    {
        var cobranca = Nova();

        cobranca.Vencida(new DateTime(2026, 11, 10)).Should().BeFalse();
        cobranca.Vencida(new DateTime(2026, 11, 11)).Should().BeTrue();

        cobranca.RegistrarPagamento(FormaPagamento.Pix, "x");
        cobranca.Vencida(new DateTime(2026, 12, 1)).Should().BeFalse();
    }
}

public class PacienteConvenioTests
{
    private static Paciente CriarPaciente() =>
        new(new Pessoa("Maria Souza", new Cpf("52998224725"), new DateTime(1980, 1, 1), new Telefone("62999990000")));

    [Fact]
    public void DefinirConvenio_DeveGuardarCarteirinhaEValidade()
    {
        var paciente = CriarPaciente();

        paciente.DefinirConvenio(2, " 0001234 ", new DateTime(2026, 12, 31));

        paciente.IdConvenio.Should().Be(2);
        paciente.NumeroCarteirinha.Should().Be("0001234");
        paciente.CarteirinhaVencida(new DateTime(2026, 12, 31)).Should().BeFalse();
        paciente.CarteirinhaVencida(new DateTime(2027, 1, 1)).Should().BeTrue();
        paciente.CarteirinhaVencendo(new DateTime(2026, 12, 15), 30).Should().BeTrue();
        paciente.CarteirinhaVencendo(new DateTime(2026, 10, 1), 30).Should().BeFalse();
    }

    [Fact]
    public void DefinirConvenio_SemNumero_DeveLancarDomainException()
    {
        var act = () => CriarPaciente().DefinirConvenio(2, "", null);

        act.Should().Throw<DomainException>().WithMessage("*carteirinha*");
    }

    [Fact]
    public void DefinirConvenio_Nulo_DeveTornarParticular()
    {
        var paciente = CriarPaciente();
        paciente.DefinirConvenio(2, "123456", null);

        paciente.DefinirConvenio(null, "ignorado", DateTime.Today);

        paciente.IdConvenio.Should().BeNull();
        paciente.NumeroCarteirinha.Should().BeNull();
        paciente.CarteirinhaVencida(DateTime.Today.AddYears(5)).Should().BeFalse();
    }
}

public class ConsultaRetornoTests
{
    [Fact]
    public void DefinirRetorno_DeveAceitarDataFuturaEZerarLembreteAoMudar()
    {
        var consulta = new Consulta(1, 2, "Evolução");
        var hoje = consulta.DtCriacao.Date;

        consulta.DefinirRetorno(hoje.AddDays(30));
        consulta.MarcarLembreteRetornoEnviado();
        consulta.RetornoEm.Should().Be(hoje.AddDays(30));

        consulta.DefinirRetorno(hoje.AddDays(45));
        consulta.LembreteRetornoEnviadoEm.Should().BeNull();

        consulta.DefinirRetorno(null);
        consulta.RetornoEm.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(400)]
    public void DefinirRetorno_ForaDoIntervalo_DeveLancarDomainException(int dias)
    {
        var consulta = new Consulta(1, 2, "Evolução");

        var act = () => consulta.DefinirRetorno(consulta.DtCriacao.Date.AddDays(dias));

        act.Should().Throw<DomainException>();
    }
}

public class ExecucaoRotinaTests
{
    [Fact]
    public void Registrar_DeveIndicarPeriodoJaExecutado()
    {
        var execucao = new ExecucaoRotina("resumo-diario", "2026-11-10");

        execucao.JaExecutou("2026-11-10").Should().BeTrue();
        execucao.JaExecutou("2026-11-11").Should().BeFalse();

        execucao.Registrar("2026-11-11");
        execucao.JaExecutou("2026-11-11").Should().BeTrue();
    }
}
