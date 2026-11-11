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

public class ProntuarioAppServiceTests
{
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IConsultaRepositorio _consultaRepositorio = Substitute.For<IConsultaRepositorio>();
    private readonly IExameRepositorio _exameRepositorio = Substitute.For<IExameRepositorio>();
    private readonly ProntuarioAppService _sut;

    public ProntuarioAppServiceTests()
    {
        _sut = new ProntuarioAppService(
            _pacienteRepositorio,
            _medicoRepositorio,
            _consultaRepositorio,
            _exameRepositorio
        );
    }

    private static Paciente CriarPacienteMock(int id = 1, string nome = "Carlos Silva")
    {
        var pessoa = new Pessoa(nome, new Cpf("12345678909"), new DateTime(1985, 5, 20), new Telefone("11988887777"));
        typeof(Entidade).GetProperty("Id")!.SetValue(pessoa, id);

        var paciente = new Paciente(pessoa);
        typeof(Entidade).GetProperty("Id")!.SetValue(paciente, id);
        return paciente;
    }

    private static Medico CriarMedicoMock(int id = 1, string nome = "Dr. Roberto", string crm = "123456/SP")
    {
        var pessoa = new Pessoa(nome, new Cpf("98765432100"), new DateTime(1975, 3, 15), new Telefone("11977776666"));
        typeof(Entidade).GetProperty("Id")!.SetValue(pessoa, id);

        var medico = new Medico(pessoa, new Crm(crm), [Especialidade.Cardiologista, Especialidade.ClinicoGeral]);
        typeof(Entidade).GetProperty("Id")!.SetValue(medico, id);
        return medico;
    }

    [Fact]
    public async Task GerarProntuarioAsync_QuandoPacienteExiste_DeveRetornarProntuarioCompleto()
    {
        // Arrange
        var paciente = CriarPacienteMock(1, "Carlos Silva");
        var medico = CriarMedicoMock(1, "Dr. Roberto", "123456/SP");

        var consulta = new Consulta(1, 1, "Paciente relata dor torácica aos esforços.");
        typeof(Consulta).GetProperty("Medico")!.SetValue(consulta, medico);
        typeof(Consulta).GetProperty("Paciente")!.SetValue(consulta, paciente);

        var exame1 = new Exame(1, "Eletrocardiograma", "Ritmo sinusal normal sem alterações.", null, 1);
        typeof(Exame).GetProperty("Medico")!.SetValue(exame1, medico);

        var exame2 = new Exame(1, "Ecocardiograma Transtorácico", null, null, 1);
        typeof(Exame).GetProperty("Medico")!.SetValue(exame2, medico);

        _pacienteRepositorio.ObterDetalhadoPorId(1).Returns(paciente);
        _consultaRepositorio.ObterPorPacienteFiltrado(1, null, null, null).Returns([consulta]);
        _exameRepositorio.ObterPorPacienteFiltrado(1, null, null, null).Returns([exame1, exame2]);

        var filtro = new ProntuarioFiltroRequest(1);

        // Act
        var result = await _sut.GerarProntuarioAsync(filtro);

        // Assert
        result.Should().NotBeNull();
        result.NumeroOficio.Should().StartWith("OF-");
        result.Paciente.Nome.Should().Be("Carlos Silva");
        result.Resumo.TotalConsultas.Should().Be(1);
        result.Resumo.TotalExames.Should().Be(2);
        result.Resumo.TotalLaudosEmitidos.Should().Be(1);
        result.Resumo.TotalExamesPendentes.Should().Be(1);
        result.Consultas.Should().HaveCount(1);
        result.Consultas[0].NomeMedico.Should().Be("Dr. Roberto");
        result.Exames.Should().HaveCount(2);
        result.Exames.Should().Contain(e => e.Descricao == "Eletrocardiograma" && e.PossuiLaudo);
        result.Exames.Should().Contain(e => e.Descricao == "Ecocardiograma Transtorácico" && !e.PossuiLaudo);
    }

    [Fact]
    public async Task GerarProntuarioAsync_ComFiltroMedicoEPeriodo_DeveRetornarDadosFiltrados()
    {
        // Arrange
        var paciente = CriarPacienteMock(2, "Mariana Lima");
        var medico = CriarMedicoMock(5, "Dra. Ana Paula", "654321/SP");
        var dataInicio = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dataFim = new DateTime(2026, 6, 30, 0, 0, 0, DateTimeKind.Utc);

        _pacienteRepositorio.ObterDetalhadoPorId(2).Returns(paciente);
        _medicoRepositorio.ObterDetalhadoPorId(5).Returns(medico);
        _consultaRepositorio.ObterPorPacienteFiltrado(2, 5, dataInicio, dataFim).Returns([]);
        _exameRepositorio.ObterPorPacienteFiltrado(2, 5, dataInicio, dataFim).Returns([]);

        var filtro = new ProntuarioFiltroRequest(2, IdMedico: 5, DataInicio: dataInicio, DataFim: dataFim);

        // Act
        var result = await _sut.GerarProntuarioAsync(filtro);

        // Assert
        result.MedicoFiltro.Should().NotBeNull();
        result.MedicoFiltro!.Nome.Should().Be("Dra. Ana Paula");
        result.DataInicio.Should().Be(dataInicio);
        result.DataFim.Should().Be(dataFim);
        await _consultaRepositorio.Received(1).ObterPorPacienteFiltrado(2, 5, dataInicio, dataFim);
        await _exameRepositorio.Received(1).ObterPorPacienteFiltrado(2, 5, dataInicio, dataFim);
    }

    [Fact]
    public async Task GerarProntuarioAsync_QuandoDataInicioMaiorQueDataFim_DeveLancarDomainException()
    {
        // Arrange
        var filtro = new ProntuarioFiltroRequest(
            IdPaciente: 1,
            DataInicio: new DateTime(2026, 10, 1),
            DataFim: new DateTime(2026, 5, 1)
        );

        // Act
        var act = () => _sut.GerarProntuarioAsync(filtro);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*data inicial não pode ser maior que a data final*");
    }

    [Fact]
    public async Task GerarProntuarioAsync_QuandoPacienteNaoExiste_DeveLancarDomainException()
    {
        // Arrange
        _pacienteRepositorio.ObterDetalhadoPorId(99).Returns((Paciente?)null);
        var filtro = new ProntuarioFiltroRequest(99);

        // Act
        var act = () => _sut.GerarProntuarioAsync(filtro);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Paciente com id 99 não encontrado*");
    }

    [Fact]
    public async Task GerarProntuarioAsync_QuandoMedicoFiltroNaoExiste_DeveLancarDomainException()
    {
        // Arrange
        var paciente = CriarPacienteMock(1);
        _pacienteRepositorio.ObterDetalhadoPorId(1).Returns(paciente);
        _medicoRepositorio.ObterDetalhadoPorId(88).Returns((Medico?)null);

        var filtro = new ProntuarioFiltroRequest(1, IdMedico: 88);

        // Act
        var act = () => _sut.GerarProntuarioAsync(filtro);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Médico com id 88 não encontrado*");
    }
}
