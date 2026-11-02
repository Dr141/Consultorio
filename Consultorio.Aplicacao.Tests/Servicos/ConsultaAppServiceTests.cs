using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class ConsultaAppServiceTests
{
    private readonly IConsultaRepositorio _consultaRepositorio = Substitute.For<IConsultaRepositorio>();
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IAgendaRepositorio _agendaRepositorio = Substitute.For<IAgendaRepositorio>();
    private readonly ConsultaAppService _sut;

    public ConsultaAppServiceTests()
    {
        _sut = new ConsultaAppService(_consultaRepositorio, _pacienteRepositorio, _medicoRepositorio, _agendaRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoPacienteNaoExiste_DeveLancarDomainException()
    {
        var request = new ConsultaRequest(IdPaciente: 99, IdMedico: 1, Observacao: "Rotina", IdAgenda: null);
        _pacienteRepositorio.ObterId(99).Returns((Paciente?)null);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Paciente com id 99 não encontrado*");
    }

    [Fact]
    public async Task AdicionarAsync_ComDadosValidos_DeveCadastrarConsulta()
    {
        var request = new ConsultaRequest(IdPaciente: 1, IdMedico: 2, Observacao: "Primeira consulta", IdAgenda: null);
        _pacienteRepositorio.ObterId(1).Returns(Substitute.For<Paciente>());
        _medicoRepositorio.ObterId(2).Returns(Substitute.For<Medico>());
        _consultaRepositorio.Adicionar(Arg.Any<Consulta>()).Returns(call => call.Arg<Consulta>());

        var response = await _sut.AdicionarAsync(request);

        response.Observacao.Should().Be("Primeira consulta");
        await _consultaRepositorio.Received(1).Adicionar(Arg.Any<Consulta>());
    }
}
