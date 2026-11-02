using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class AgendaAppServiceTests
{
    private readonly IAgendaRepositorio _agendaRepositorio = Substitute.For<IAgendaRepositorio>();
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly AgendaAppService _sut;

    public AgendaAppServiceTests()
    {
        _sut = new AgendaAppService(_agendaRepositorio, _medicoRepositorio, _pacienteRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoExisteConflitoHorario_DeveLancarDomainException()
    {
        var data = DateTime.UtcNow.Date.AddDays(1);
        var hora = new TimeSpan(10, 0, 0);
        var request = new AgendaRequest(data, hora, IdMedico: 1, IdPaciente: null, IdConsulta: null, IdExame: null);

        _medicoRepositorio.ObterId(1).Returns(Substitute.For<Medico>());
        _agendaRepositorio.ExisteConflitoHorario(1, data, hora).Returns(true);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*já possui agendamento*");
    }

    [Fact]
    public async Task AgendarPacienteAsync_QuandoHorarioDisponivel_DeveVincularPaciente()
    {
        var agenda = new Agenda(DateTime.UtcNow.Date.AddDays(1), new TimeSpan(11, 0, 0), idMedico: 1);
        _agendaRepositorio.ObterDetalhadoPorId(10).Returns(agenda);
        _pacienteRepositorio.ObterId(5).Returns(Substitute.For<Paciente>());
        _agendaRepositorio.Atualizar(Arg.Any<Agenda>()).Returns(call => call.Arg<Agenda>());

        var response = await _sut.AgendarPacienteAsync(10, 5);

        agenda.IdPaciente.Should().Be(5);
        agenda.Disponivel.Should().BeFalse();
    }
}
