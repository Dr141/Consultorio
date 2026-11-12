using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class RotinasClinicaAppServiceTests
{
    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private readonly IFinanceiroAppService _financeiro = Substitute.For<IFinanceiroAppService>();
    private readonly IIndicadoresAppService _indicadores = Substitute.For<IIndicadoresAppService>();
    private readonly IExameRepositorio _exames = Substitute.For<IExameRepositorio>();
    private readonly IConsultaRepositorio _consultas = Substitute.For<IConsultaRepositorio>();
    private readonly IAgendaRepositorio _agendas = Substitute.For<IAgendaRepositorio>();
    private readonly IMedicoRepositorio _medicos = Substitute.For<IMedicoRepositorio>();
    private readonly IExecucaoRotinaRepositorio _execucoes = Substitute.For<IExecucaoRotinaRepositorio>();
    private readonly INotificador _notificador = Substitute.For<INotificador>();
    private readonly OpcoesClinica _opcoes = new() { FusoHorario = "Inexistente/Fixo", Nome = "Clínica Teste", HoraResumoDiario = 7 };

    public RotinasClinicaAppServiceTests()
    {
        _notificador.PodeEnviar(Arg.Any<MensagemNotificacao>()).Returns(call => call.Arg<MensagemNotificacao>().Email is not null);
        _notificador.EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>()).Returns(true);
        _exames.Atualizar(Arg.Any<Exame>()).Returns(call => call.Arg<Exame>());
        _medicos.ObterTodosDetalhado().Returns(new List<Medico>());
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>()).Returns(new List<Agenda>());
        _exames.ObterEmAndamento().Returns(new List<Exame>());
    }

    private RotinasClinicaAppService CriarSut(DateTimeOffset agora) =>
        new(_financeiro, _indicadores, _exames, _consultas, _agendas, _medicos, _execucoes, [_notificador], _opcoes, new RelogioFixo(agora));

    private static Exame ExameLaudadoDe(string? email)
    {
        var pessoa = new Pessoa("Joana Prado", new Cpf("52998224725"), new DateTime(1990, 1, 1), new Telefone("62999990000"));
        pessoa.DefinirEmail(email);
        var exame = new Exame(7, "Hemograma completo", "Sem alterações");
        typeof(Exame).GetProperty(nameof(Exame.Paciente))!.SetValue(exame, new Paciente(pessoa));
        return exame;
    }

    [Fact]
    public async Task AvisarLaudosProntos_DeveAvisarSemExporOResultado()
    {
        var exame = ExameLaudadoDe("joana@email.com");
        _exames.ObterEmAndamento().Returns(new List<Exame> { exame });

        var resultado = await CriarSut(DateTimeOffset.UtcNow).AvisarLaudosProntosAsync();

        resultado.Enviados.Should().Be(1);
        exame.AvisoLaudoEnviadoEm.Should().NotBeNull();
        await _notificador.Received(1).EnviarAsync(
            Arg.Is<MensagemNotificacao>(m => m.Email == "joana@email.com" && !m.Texto.Contains("Sem alterações") && !m.Html!.Contains("Sem alterações")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AvisarLaudosProntos_JaAvisado_NaoDeveReenviar()
    {
        var exame = ExameLaudadoDe("joana@email.com");
        exame.MarcarAvisoLaudoEnviado();
        _exames.ObterEmAndamento().Returns(new List<Exame> { exame });

        var resultado = await CriarSut(DateTimeOffset.UtcNow).AvisarLaudosProntosAsync();

        resultado.Processados.Should().Be(0);
        await _notificador.DidNotReceiveWithAnyArgs().EnviarAsync(default!, default);
    }

    [Fact]
    public async Task ResumoDiario_AntesDaHoraConfigurada_NaoDeveExecutar()
    {
        // 09:00 UTC = 06:00 em Brasília
        var enviou = await CriarSut(new DateTimeOffset(2027, 3, 10, 9, 0, 0, TimeSpan.Zero)).EnviarResumoDiarioAsync();

        enviou.Should().BeFalse();
        await _execucoes.DidNotReceiveWithAnyArgs().ObterPorNome(default!);
    }

    [Fact]
    public async Task ResumoDiario_JaExecutadoNoDia_NaoDeveRepetir()
    {
        _execucoes.ObterPorNome(RotinasClinicaAppService.RotinaResumoDiario)
            .Returns(new ExecucaoRotina(RotinasClinicaAppService.RotinaResumoDiario, "2027-03-10"));

        // 12:00 UTC = 09:00 em Brasília
        var enviou = await CriarSut(new DateTimeOffset(2027, 3, 10, 12, 0, 0, TimeSpan.Zero)).EnviarResumoDiarioAsync();

        enviou.Should().BeFalse();
        await _medicos.DidNotReceive().ObterTodosDetalhado();
    }

    [Fact]
    public async Task ResumoDiario_PrimeiraExecucaoDoDia_DeveRegistrarExecucao()
    {
        _execucoes.ObterPorNome(RotinasClinicaAppService.RotinaResumoDiario)
            .Returns(new ExecucaoRotina(RotinasClinicaAppService.RotinaResumoDiario, "2027-03-09"));

        var enviou = await CriarSut(new DateTimeOffset(2027, 3, 10, 12, 0, 0, TimeSpan.Zero)).EnviarResumoDiarioAsync();

        enviou.Should().BeTrue();
        await _execucoes.Received(1).Atualizar(Arg.Is<ExecucaoRotina>(e => e.Referencia == "2027-03-10"));
    }
}
