using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Servicos;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class AgendaFluxoAtendimentoTests
{
    private static Agenda NovoAgendamento() =>
        new(new DateTime(2027, 3, 10), new TimeSpan(9, 0, 0), idMedico: 1, idPaciente: 5);

    [Fact]
    public void AgendarPaciente_DeveIniciarComoAgendadoComTokenDeConfirmacao()
    {
        var agenda = NovoAgendamento();

        agenda.Status.Should().Be(StatusAgendamento.Agendado);
        agenda.TokenConfirmacao.Should().NotBeNull();
        agenda.Disponivel.Should().BeFalse();
    }

    [Fact]
    public void FluxoCompleto_DeveRegistrarHorariosDeCadaEtapa()
    {
        var agenda = NovoAgendamento();

        agenda.Confirmar();
        agenda.RegistrarChegada();
        agenda.IniciarAtendimento();
        agenda.ConcluirAtendimento();

        agenda.Status.Should().Be(StatusAgendamento.Atendido);
        agenda.DtConfirmacao.Should().NotBeNull();
        agenda.DtChegada.Should().NotBeNull();
        agenda.DtInicioAtendimento.Should().NotBeNull();
        agenda.DtFimAtendimento.Should().NotBeNull();
    }

    [Fact]
    public void LiberarHorario_ComAtendimentoIniciado_DeveSerRecusado()
    {
        var agenda = NovoAgendamento();
        agenda.IniciarAtendimento();

        var act = () => agenda.LiberarHorario();

        act.Should().Throw<DomainException>().WithMessage("*já foi iniciado*");
    }

    [Fact]
    public void LiberarHorario_DeveLimparSituacaoEToken()
    {
        var agenda = NovoAgendamento();
        agenda.Confirmar();

        agenda.LiberarHorario();

        agenda.Disponivel.Should().BeTrue();
        agenda.Status.Should().BeNull();
        agenda.TokenConfirmacao.Should().BeNull();
        agenda.DtConfirmacao.Should().BeNull();
    }

    [Fact]
    public void RegistrarFalta_DepoisDaChegada_DeveSerRecusado()
    {
        var agenda = NovoAgendamento();
        agenda.RegistrarChegada();

        var act = () => agenda.RegistrarFalta();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RegistrarChegada_DePacienteMarcadoComoFalta_DevePermitir()
    {
        var agenda = NovoAgendamento();
        agenda.RegistrarFalta();

        agenda.RegistrarChegada();

        agenda.Status.Should().Be(StatusAgendamento.Aguardando);
    }

    [Fact]
    public void Confirmar_HorarioLivre_DeveSerRecusado()
    {
        var agenda = new Agenda(new DateTime(2027, 3, 10), new TimeSpan(9, 0, 0), idMedico: 1);

        var act = () => agenda.Confirmar();

        act.Should().Throw<DomainException>().WithMessage("*livre*");
    }
}

public class GradeHorariosTests
{
    private static ParametrosGrade Grade(DateTime inicio, DateTime fim, params DayOfWeek[] dias) =>
        new(inicio, fim, dias, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), 30,
            PausaInicio: new TimeSpan(10, 0, 0), PausaFim: new TimeSpan(10, 30, 0));

    [Fact]
    public void HorariosDoDia_DevePularPausa()
    {
        var horarios = GradeHorarios.HorariosDoDia(Grade(DateTime.Today, DateTime.Today, DayOfWeek.Monday)).ToList();

        horarios.Should().HaveCount(7); // 8h às 12h de 30 em 30 = 8, menos o das 10h
        horarios.Should().NotContain(new TimeSpan(10, 0, 0));
        horarios.Should().Contain(new TimeSpan(11, 30, 0));
    }

    [Fact]
    public void Gerar_SoNosDiasSelecionados_IgnorandoExistentesEBloqueios()
    {
        // 01/03/2027 é segunda-feira
        var segunda = new DateTime(2027, 3, 1);
        var parametros = Grade(segunda, segunda.AddDays(13), DayOfWeek.Monday, DayOfWeek.Wednesday);
        var bloqueio = new BloqueioAgenda(null, segunda.AddDays(7), segunda.AddDays(7), "Feriado municipal");
        var existentes = new[] { (segunda, new TimeSpan(8, 0, 0)) };

        var (novos, jaExistiam, bloqueados) = GradeHorarios.Gerar(parametros, idMedico: 1, [bloqueio], existentes);

        // 4 dias úteis selecionados (2 seg + 2 qua); a segunda da semana 2 está bloqueada.
        novos.Select(n => n.Data.Date).Distinct().Should().HaveCount(3);
        novos.Should().HaveCount(3 * 7 - 1);
        jaExistiam.Should().Be(1);
        bloqueados.Should().Be(7);
        novos.Should().OnlyContain(n => n.Data.DayOfWeek == DayOfWeek.Monday || n.Data.DayOfWeek == DayOfWeek.Wednesday);
    }

    [Fact]
    public void Gerar_BloqueioDeOutroMedico_NaoDeveAfetar()
    {
        var segunda = new DateTime(2027, 3, 1);
        var bloqueioOutro = new BloqueioAgenda(99, segunda, segunda, "Congresso");

        var (novos, _, bloqueados) = GradeHorarios.Gerar(Grade(segunda, segunda, DayOfWeek.Monday), 1, [bloqueioOutro], []);

        novos.Should().HaveCount(7);
        bloqueados.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(300)]
    public void Validar_IntervaloForaDoLimite_DeveLancarDomainException(int intervalo)
    {
        var p = new ParametrosGrade(DateTime.Today, DateTime.Today, [DayOfWeek.Monday], new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), intervalo);

        var act = () => GradeHorarios.Validar(p);

        act.Should().Throw<DomainException>().WithMessage("*intervalo*");
    }
}
