using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class RotinasAgendaAppServiceTests
{
    /// <summary>Relógio fixo para testar regras de horário.</summary>
    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    private readonly IAgendaRepositorio _agendaRepositorio = Substitute.For<IAgendaRepositorio>();
    private readonly INotificador _notificador = Substitute.For<INotificador>();
    // Fuso fixo UTC-3 para o teste não depender da base de fusos da máquina.
    private readonly OpcoesClinica _opcoes = new() { FusoHorario = "Inexistente/Fixo", Nome = "Clínica Teste" };

    // 10/03/2027 15:00 em Brasília = 18:00 UTC
    private static readonly DateTimeOffset Agora = new(2027, 3, 10, 18, 0, 0, TimeSpan.Zero);

    public RotinasAgendaAppServiceTests()
    {
        _notificador.PodeEnviar(Arg.Any<MensagemNotificacao>()).Returns(call => call.Arg<MensagemNotificacao>().Email is not null);
        _notificador.EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>()).Returns(true);
        _agendaRepositorio.Atualizar(Arg.Any<Agenda>()).Returns(call => call.Arg<Agenda>());
    }

    private RotinasAgendaAppService CriarSut() =>
        new(_agendaRepositorio, [_notificador], _opcoes, new RelogioFixo(Agora));

    private static Agenda AgendamentoCom(DateTime data, TimeSpan hora, string? email)
    {
        var pessoa = new Pessoa("Joana Prado", new Cpf("52998224725"), new DateTime(1990, 1, 1), new Telefone("62999990000"));
        pessoa.DefinirEmail(email);
        var paciente = new Paciente(pessoa);
        var agenda = new Agenda(data, hora, idMedico: 1, idPaciente: 7);
        typeof(Agenda).GetProperty(nameof(Agenda.Paciente))!.SetValue(agenda, paciente);
        return agenda;
    }

    [Fact]
    public async Task EnviarLembretes_DeveEnviarSoParaQuemTemAtendimentoNaJanelaETemContato()
    {
        var amanhaComEmail = AgendamentoCom(new DateTime(2027, 3, 11), new TimeSpan(9, 0, 0), "joana@email.com");
        var amanhaSemEmail = AgendamentoCom(new DateTime(2027, 3, 11), new TimeSpan(10, 0, 0), null);
        var depoisDaJanela = AgendamentoCom(new DateTime(2027, 3, 11), new TimeSpan(16, 0, 0), "outro@email.com");
        _agendaRepositorio.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { amanhaComEmail, amanhaSemEmail, depoisDaJanela });

        var resultado = await CriarSut().EnviarLembretesAsync();

        resultado.Processados.Should().Be(2);
        resultado.Enviados.Should().Be(1);
        resultado.SemContato.Should().Be(1);
        amanhaComEmail.LembreteEnviadoEm.Should().NotBeNull();
        depoisDaJanela.LembreteEnviadoEm.Should().BeNull();
        await _notificador.Received(1).EnviarAsync(
            Arg.Is<MensagemNotificacao>(m => m.Email == "joana@email.com" && m.Texto.Contains("/api/publico/confirmacao/")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EnviarLembretes_NaoDeveReenviarNemEnviarParaConfirmados()
    {
        var jaLembrado = AgendamentoCom(new DateTime(2027, 3, 11), new TimeSpan(9, 0, 0), "a@email.com");
        jaLembrado.MarcarLembreteEnviado();
        var confirmado = AgendamentoCom(new DateTime(2027, 3, 11), new TimeSpan(9, 30, 0), "b@email.com");
        confirmado.Confirmar();
        _agendaRepositorio.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { jaLembrado, confirmado });

        var resultado = await CriarSut().EnviarLembretesAsync();

        resultado.Processados.Should().Be(0);
        await _notificador.DidNotReceive().EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarcarFaltas_DeveMarcarSoQuemPassouDaTolerancia()
    {
        _opcoes.ToleranciaFaltaMinutos = 120;
        var manhaSemCheckin = AgendamentoCom(new DateTime(2027, 3, 10), new TimeSpan(9, 0, 0), null);      // 9h + 2h < 15h
        var recente = AgendamentoCom(new DateTime(2027, 3, 10), new TimeSpan(14, 0, 0), null);             // 14h + 2h > 15h
        var chegou = AgendamentoCom(new DateTime(2027, 3, 10), new TimeSpan(8, 0, 0), null);
        chegou.RegistrarChegada();
        _agendaRepositorio.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>())
            .Returns(new List<Agenda> { manhaSemCheckin, recente, chegou });

        var faltas = await CriarSut().MarcarFaltasAsync();

        faltas.Should().Be(1);
        manhaSemCheckin.Status.Should().Be(StatusAgendamento.Faltou);
        recente.Status.Should().Be(StatusAgendamento.Agendado);
        chegou.Status.Should().Be(StatusAgendamento.Aguardando);
    }
}
