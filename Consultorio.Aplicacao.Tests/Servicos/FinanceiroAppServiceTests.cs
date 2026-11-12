using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class FinanceiroAppServiceTests
{
    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    // 10/03/2027 15:00 em Brasília = 18:00 UTC
    private static readonly DateTimeOffset Agora = new(2027, 3, 10, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTime Hoje = new(2027, 3, 10);

    private readonly ICobrancaRepositorio _cobrancas = Substitute.For<ICobrancaRepositorio>();
    private readonly IPacienteRepositorio _pacientes = Substitute.For<IPacienteRepositorio>();
    private readonly IAgendaRepositorio _agendas = Substitute.For<IAgendaRepositorio>();
    private readonly ITabelaPrecoRepositorio _precos = Substitute.For<ITabelaPrecoRepositorio>();
    private readonly IConvenioRepositorio _convenios = Substitute.For<IConvenioRepositorio>();
    private readonly OpcoesClinica _opcoes = new() { FusoHorario = "Inexistente/Fixo", Nome = "Clínica Teste", PrazoPagamentoConvenioDias = 30 };

    public FinanceiroAppServiceTests()
    {
        _cobrancas.Adicionar(Arg.Any<Cobranca>()).Returns(call => call.Arg<Cobranca>());
        _cobrancas.Atualizar(Arg.Any<Cobranca>()).Returns(call => call.Arg<Cobranca>());
        _cobrancas.AgendasComCobranca(Arg.Any<IEnumerable<int>>()).Returns(new HashSet<int>());
    }

    private FinanceiroAppService CriarSut() =>
        new(_cobrancas, _pacientes, _agendas, _precos, _convenios, _opcoes, new RelogioFixo(Agora));

    private static void DefinirId(Entidade e, int id) => typeof(Entidade).GetProperty(nameof(Entidade.Id))!.SetValue(e, id);

    private static Agenda Atendimento(int idAgenda, Paciente paciente, TipoAtendimento tipo = TipoAtendimento.Consulta)
    {
        var agenda = new Agenda(Hoje, new TimeSpan(9, 0, 0), idMedico: 1, idPaciente: paciente.Id, tipo: tipo);
        agenda.RegistrarChegada();
        agenda.ConcluirAtendimento();
        DefinirId(agenda, idAgenda);
        typeof(Agenda).GetProperty(nameof(Agenda.Paciente))!.SetValue(agenda, paciente);
        return agenda;
    }

    private static Paciente Paciente(int id, int? idConvenio = null, DateTime? validade = null)
    {
        var paciente = new Paciente(new Pessoa("Joana Prado", new Cpf("52998224725"), new DateTime(1990, 1, 1), new Telefone("62999990000")));
        DefinirId(paciente, id);
        if (idConvenio.HasValue) paciente.DefinirConvenio(idConvenio, "CART-001", validade);
        return paciente;
    }

    [Fact]
    public async Task GerarCobrancas_PacienteParticular_DeveUsarPrecoParticularComVencimentoHoje()
    {
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { Atendimento(10, Paciente(5)) });
        _precos.ObterPor(TipoAtendimento.Consulta, null).Returns(new TabelaPreco(TipoAtendimento.Consulta, null, 250m));

        var criadas = await CriarSut().GerarCobrancasDeAtendimentosAsync(Hoje.AddDays(-7), Hoje);

        criadas.Should().Be(1);
        await _cobrancas.Received(1).Adicionar(Arg.Is<Cobranca>(c =>
            c.IdPaciente == 5 && c.IdAgenda == 10 && c.Valor == 250m && c.IdConvenio == null && c.DtVencimento == Hoje));
    }

    [Fact]
    public async Task GerarCobrancas_PacienteComConvenioValido_DeveCobrarDoConvenio()
    {
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { Atendimento(11, Paciente(6, idConvenio: 3, validade: Hoje.AddMonths(6))) });
        _precos.ObterPor(TipoAtendimento.Consulta, 3).Returns(new TabelaPreco(TipoAtendimento.Consulta, 3, 120m));
        _precos.ObterPor(TipoAtendimento.Consulta, null).Returns(new TabelaPreco(TipoAtendimento.Consulta, null, 250m));

        await CriarSut().GerarCobrancasDeAtendimentosAsync(Hoje.AddDays(-7), Hoje);

        await _cobrancas.Received(1).Adicionar(Arg.Is<Cobranca>(c =>
            c.IdConvenio == 3 && c.Valor == 120m && c.DtVencimento == Hoje.AddDays(30)));
    }

    [Fact]
    public async Task GerarCobrancas_CarteirinhaVencida_DeveCobrarComoParticular()
    {
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { Atendimento(12, Paciente(7, idConvenio: 3, validade: Hoje.AddDays(-1))) });
        _precos.ObterPor(TipoAtendimento.Consulta, 3).Returns(new TabelaPreco(TipoAtendimento.Consulta, 3, 120m));
        _precos.ObterPor(TipoAtendimento.Consulta, null).Returns(new TabelaPreco(TipoAtendimento.Consulta, null, 250m));

        await CriarSut().GerarCobrancasDeAtendimentosAsync(Hoje.AddDays(-7), Hoje);

        await _cobrancas.Received(1).Adicionar(Arg.Is<Cobranca>(c => c.IdConvenio == null && c.Valor == 250m));
    }

    [Fact]
    public async Task GerarCobrancas_SemPrecoOuJaCobrado_NaoDeveCriar()
    {
        var semPreco = Atendimento(13, Paciente(8), TipoAtendimento.Retorno);
        var jaCobrado = Atendimento(14, Paciente(9));
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { semPreco, jaCobrado });
        _cobrancas.AgendasComCobranca(Arg.Any<IEnumerable<int>>()).Returns(new HashSet<int> { 14 });
        _precos.ObterPor(TipoAtendimento.Consulta, null).Returns(new TabelaPreco(TipoAtendimento.Consulta, null, 250m));

        var criadas = await CriarSut().GerarCobrancasDeAtendimentosAsync(Hoje.AddDays(-7), Hoje);

        criadas.Should().Be(0);
        await _cobrancas.DidNotReceive().Adicionar(Arg.Any<Cobranca>());
    }

    [Fact]
    public async Task GerarCobrancas_Desligado_NaoDeveConsultarAgenda()
    {
        _opcoes.GerarCobrancaAutomatica = false;

        var criadas = await CriarSut().GerarCobrancasDeAtendimentosAsync(Hoje.AddDays(-7), Hoje);

        criadas.Should().Be(0);
        await _agendas.DidNotReceiveWithAnyArgs().ObterPorPeriodo(default, default, default);
    }

    [Fact]
    public async Task GerarDoAtendimento_SemPreco_DeveExplicarComoResolver()
    {
        _agendas.ObterDetalhadoPorId(15).Returns(Atendimento(15, Paciente(10), TipoAtendimento.Procedimento));

        var act = () => CriarSut().GerarDoAtendimentoAsync(15, new UsuarioAtual("recepcao@clinica.com", false));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*tabela de preços*");
    }

    [Fact]
    public async Task RegistrarPagamento_ComDesconto_DeveQuitar()
    {
        var cobranca = new Cobranca(5, "Consulta", 200m, Hoje, "recepcao@clinica.com");
        _cobrancas.ObterDetalhadoPorId(20).Returns(cobranca);

        var resposta = await CriarSut().RegistrarPagamentoAsync(20, new PagamentoRequest(FormaPagamento.Pix, 25m),
            new UsuarioAtual("caixa@clinica.com", false));

        resposta.Status.Should().Be("Paga");
        resposta.ValorFinal.Should().Be(175m);
        resposta.FormaPagamentoNome.Should().Be("Pix");
        resposta.RecebidoPor.Should().Be("caixa@clinica.com");
    }

    [Fact]
    public async Task Recibo_CobrancaPendente_DeveSerRecusado()
    {
        _cobrancas.ObterDetalhadoPorId(21).Returns(new Cobranca(5, "Consulta", 200m, Hoje, "x"));

        var act = () => CriarSut().ObterReciboAsync(21);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*pagas*");
    }

    [Fact]
    public void MontarResumo_DeveSomarRecebidoPendenteEVencido()
    {
        var paga1 = new Cobranca(1, "Item A", 100m, Hoje, "x");
        paga1.RegistrarPagamento(FormaPagamento.Pix, "x");
        var paga2 = new Cobranca(1, "Item B", 300m, Hoje, "x");
        paga2.RegistrarPagamento(FormaPagamento.Dinheiro, "x");
        var estornada = new Cobranca(1, "Item C", 50m, Hoje, "x");
        estornada.RegistrarPagamento(FormaPagamento.Pix, "x");
        estornada.Estornar("Cobrança indevida");
        var vencida = new Cobranca(1, "Item D", 80m, Hoje.AddDays(-3), "x");
        var aVencer = new Cobranca(1, "Item E", 20m, Hoje.AddDays(3), "x");

        var resumo = FinanceiroAppService.MontarResumo([paga1, paga2, estornada], [vencida, aVencer], Hoje);

        resumo.Recebido.Should().Be(400m);
        resumo.QuantidadeRecebida.Should().Be(2);
        resumo.TicketMedio.Should().Be(200m);
        resumo.Estornado.Should().Be(50m);
        resumo.AReceber.Should().Be(100m);
        resumo.Vencido.Should().Be(80m);
        resumo.PorForma.Should().HaveCount(2);
        resumo.PorConvenio.Should().ContainSingle(i => i.Rotulo == "Particular" && i.Valor == 400m);
    }
}

public class ValorPorExtensoTests
{
    [Theory]
    [InlineData(1, "um real")]
    [InlineData(0.01, "um centavo")]
    [InlineData(2.5, "dois reais e cinquenta centavos")]
    [InlineData(100, "cem reais")]
    [InlineData(150.5, "cento e cinquenta reais e cinquenta centavos")]
    [InlineData(1000, "mil reais")]
    [InlineData(1500, "mil e quinhentos reais")]
    [InlineData(1550, "mil quinhentos e cinquenta reais")]
    [InlineData(2005, "dois mil e cinco reais")]
    [InlineData(21345.99, "vinte e um mil trezentos e quarenta e cinco reais e noventa e nove centavos")]
    [InlineData(1000000, "um milhão de reais")]
    public void Converter_DeveEscreverPorExtenso(double valor, string esperado)
    {
        ValorPorExtenso.Converter((decimal)valor).Should().Be(esperado);
    }
}
