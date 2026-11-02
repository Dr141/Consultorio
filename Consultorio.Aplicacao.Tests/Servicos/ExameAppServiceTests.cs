using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class ExameAppServiceTests
{
    private readonly IExameRepositorio _exameRepositorio = Substitute.For<IExameRepositorio>();
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IAgendaRepositorio _agendaRepositorio = Substitute.For<IAgendaRepositorio>();
    private readonly ExameAppService _sut;

    public ExameAppServiceTests()
    {
        _sut = new ExameAppService(_exameRepositorio, _pacienteRepositorio, _agendaRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoPacienteNaoExiste_DeveLancarDomainException()
    {
        var request = new ExameRequest(IdPaciente: 99, Descricao: "Eletrocardiograma", Resultado: null, IdAgenda: null);
        _pacienteRepositorio.ObterId(99).Returns((Paciente?)null);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Paciente com id 99 não encontrado*");
    }

    [Fact]
    public async Task RegistrarResultadoAsync_ComResultadoValido_DeveAtualizarResultado()
    {
        var exame = new Exame(1, "Eletrocardiograma");
        _exameRepositorio.ObterDetalhadoPorId(5).Returns(exame);
        _exameRepositorio.Atualizar(Arg.Any<Exame>()).Returns(call => call.Arg<Exame>());

        var response = await _sut.RegistrarResultadoAsync(5, "Ritmo sinusal normal.");

        exame.Resultado.Should().Be("Ritmo sinusal normal.");
    }
}
