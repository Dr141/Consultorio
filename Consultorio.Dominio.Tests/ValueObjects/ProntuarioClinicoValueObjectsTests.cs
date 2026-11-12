using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.ValueObjects;

public class SinaisVitaisTests
{
    [Fact]
    public void Criar_ComValoresValidos_DeveCalcularImc()
    {
        var sinais = new SinaisVitais(pressaoSistolica: 120, pressaoDiastolica: 80, pesoKg: 70m, alturaCm: 175);

        sinais.Imc.Should().Be(22.9m);
        sinais.Vazio.Should().BeFalse();
    }

    [Fact]
    public void Criar_SemValores_DeveSerVazioESemImc()
    {
        var sinais = new SinaisVitais();

        sinais.Vazio.Should().BeTrue();
        sinais.Imc.Should().BeNull();
    }

    [Fact]
    public void Criar_ComPressaoIncompleta_DeveLancarDomainException()
    {
        var act = () => new SinaisVitais(pressaoSistolica: 120);

        act.Should().Throw<DomainException>().WithMessage("*sistólica e diastólica*");
    }

    [Fact]
    public void Criar_ComDiastolicaMaiorQueSistolica_DeveLancarDomainException()
    {
        var act = () => new SinaisVitais(pressaoSistolica: 80, pressaoDiastolica: 120);

        act.Should().Throw<DomainException>().WithMessage("*diastólica deve ser menor*");
    }

    [Theory]
    [InlineData(10, null, null)]
    [InlineData(null, 101, null)]
    [InlineData(null, null, 260)]
    public void Criar_ForaDaFaixaFisiologica_DeveLancarDomainException(int? frequenciaCardiaca, int? saturacao, int? altura)
    {
        var act = () => new SinaisVitais(frequenciaCardiaca: frequenciaCardiaca, saturacaoO2: saturacao, alturaCm: altura);

        act.Should().Throw<DomainException>().WithMessage("*fora do intervalo*");
    }

    [Fact]
    public void Criar_ComTemperaturaImpossivel_DeveLancarDomainException()
    {
        var act = () => new SinaisVitais(temperatura: 50m);

        act.Should().Throw<DomainException>().WithMessage("Temperatura*");
    }
}

public class EvolucaoClinicaTests
{
    [Fact]
    public void Criar_SomenteComSoap_DeveAceitarObservacaoVazia()
    {
        var evolucao = new EvolucaoClinica(null, subjetivo: "  Dor de garganta há 3 dias  ", plano: "Sintomáticos");

        evolucao.Observacao.Should().BeEmpty();
        evolucao.Subjetivo.Should().Be("Dor de garganta há 3 dias");
        evolucao.Plano.Should().Be("Sintomáticos");
        evolucao.Objetivo.Should().BeNull();
    }

    [Fact]
    public void Criar_TudoVazio_DeveLancarDomainException()
    {
        var act = () => new EvolucaoClinica("   ", " ", null, "", null);

        act.Should().Throw<DomainException>().WithMessage("*ao menos um campo*");
    }

    [Fact]
    public void Criar_ComObservacaoLongaDemais_DeveLancarDomainException()
    {
        var act = () => new EvolucaoClinica(new string('a', EvolucaoClinica.TamanhoMaximoObservacao + 1));

        act.Should().Throw<DomainException>().WithMessage("*observação*");
    }

    [Fact]
    public void Criar_ComCampoSoapLongoDemais_DeveLancarDomainException()
    {
        var act = () => new EvolucaoClinica("ok", avaliacao: new string('a', EvolucaoClinica.TamanhoMaximoCampo + 1));

        act.Should().Throw<DomainException>().WithMessage("*Avaliação*");
    }
}

public class CidTests
{
    [Theory]
    [InlineData("J06.9", "J06.9")]
    [InlineData("j069", "J06.9")]
    [InlineData(" i10 ", "I10")]
    [InlineData("E11.65", "E11.65")]
    public void Criar_ComCodigoValido_DeveNormalizar(string codigo, string esperado)
    {
        var cid = new Cid(codigo, "  Descrição  ");

        cid.Codigo.Should().Be(esperado);
        cid.Descricao.Should().Be("Descrição");
    }

    [Theory]
    [InlineData("")]
    [InlineData("10J")]
    [InlineData("J6")]
    [InlineData("J06.999")]
    public void Criar_ComCodigoInvalido_DeveLancarDomainException(string codigo)
    {
        var act = () => new Cid(codigo, null);

        act.Should().Throw<DomainException>().WithMessage("*CID-10 inválido*");
    }

    [Fact]
    public void Criar_ComDescricaoLongaDemais_DeveLancarDomainException()
    {
        var act = () => new Cid("I10", new string('x', 201));

        act.Should().Throw<DomainException>().WithMessage("*descrição*");
    }
}
