using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class AgendaTests
{
    [Fact]
    public void CriarAgenda_SemPaciente_DeveEstarDisponivel()
    {
        var data = DateTime.UtcNow.Date.AddDays(1);
        var hora = new TimeSpan(14, 30, 0);

        var agenda = new Agenda(data, hora, idMedico: 1);

        agenda.Disponivel.Should().BeTrue();
        agenda.IdPaciente.Should().BeNull();
        agenda.Hora.Should().Be(hora);
    }

    [Fact]
    public void AgendarPaciente_QuandoDisponivel_DeveOcuparHorario()
    {
        var agenda = new Agenda(DateTime.UtcNow.Date.AddDays(1), new TimeSpan(10, 0, 0), idMedico: 1);

        agenda.AgendarPaciente(5);

        agenda.Disponivel.Should().BeFalse();
        agenda.IdPaciente.Should().Be(5);
    }

    [Fact]
    public void AgendarPaciente_QuandoJaOcupado_DeveLancarDomainException()
    {
        var agenda = new Agenda(DateTime.UtcNow.Date.AddDays(1), new TimeSpan(10, 0, 0), idMedico: 1, idPaciente: 5);

        var act = () => agenda.AgendarPaciente(6);

        act.Should().Throw<DomainException>()
            .WithMessage("*já está ocupado*");
    }

    [Fact]
    public void LiberarHorario_DeveTornarDisponivelERemoverVinculos()
    {
        var agenda = new Agenda(DateTime.UtcNow.Date.AddDays(1), new TimeSpan(10, 0, 0), idMedico: 1, idPaciente: 5);
        agenda.VincularConsulta(2);

        agenda.LiberarHorario();

        agenda.Disponivel.Should().BeTrue();
        agenda.IdPaciente.Should().BeNull();
        agenda.IdConsulta.Should().BeNull();
    }
}
