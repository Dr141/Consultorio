using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class ConsultaProntuarioRegrasTests
{
    private readonly IConsultaRepositorio _consultaRepositorio = Substitute.For<IConsultaRepositorio>();
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IAgendaRepositorio _agendaRepositorio = Substitute.For<IAgendaRepositorio>();
    private readonly ConsultaAppService _sut;

    public ConsultaProntuarioRegrasTests()
    {
        _sut = new ConsultaAppService(_consultaRepositorio, _pacienteRepositorio, _medicoRepositorio, _agendaRepositorio);
        _consultaRepositorio.Atualizar(Arg.Any<Consulta>()).Returns(call => call.Arg<Consulta>());
    }

    private static Consulta CriarConsultaDoMedico(string? emailMedico)
    {
        var pessoa = new Pessoa("Dr. Roberto Alves", new Cpf("52998224725"), new DateTime(1975, 3, 3), new Telefone("62999990000"));
        var medico = new Medico(pessoa, new Crm("4321/GO"), [Especialidade.Cardiologista]);
        medico.VincularUsuario(emailMedico);

        var consulta = new Consulta(1, 2, "Evolução inicial");
        // Navegação preenchida como o EF faria ao carregar a consulta com o médico.
        typeof(Consulta).GetProperty(nameof(Consulta.Medico))!.SetValue(consulta, medico);
        return consulta;
    }

    [Fact]
    public async Task RemoverAsync_DeveRecusarExclusaoDeProntuario()
    {
        var act = () => _sut.RemoverAsync(1);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Anular*");
        await _consultaRepositorio.DidNotReceive().Remover(Arg.Any<Consulta>());
    }

    [Fact]
    public async Task AtualizarObservacao_PorOutroMedico_DeveSerRecusada()
    {
        var consulta = CriarConsultaDoMedico("roberto@clinica.com");
        _consultaRepositorio.ObterDetalhadoPorId(10).Returns(consulta);

        var act = () => _sut.AtualizarObservacaoAsync(10, "Texto alterado", new UsuarioAtual("outra@clinica.com", EhAdministrador: false));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Somente o médico responsável*");
        consulta.Observacao.Should().Be("Evolução inicial");
    }

    [Fact]
    public async Task AtualizarObservacao_PeloProprioMedico_DevePermitir()
    {
        var consulta = CriarConsultaDoMedico("roberto@clinica.com");
        _consultaRepositorio.ObterDetalhadoPorId(10).Returns(consulta);

        await _sut.AtualizarObservacaoAsync(10, "Texto revisado", new UsuarioAtual("Roberto@Clinica.com", EhAdministrador: false));

        consulta.Observacao.Should().Be("Texto revisado");
    }

    [Fact]
    public async Task Anular_PorAdministrador_DevePermitirMesmoNaoSendoOAutor()
    {
        var consulta = CriarConsultaDoMedico("roberto@clinica.com");
        _consultaRepositorio.ObterDetalhadoPorId(10).Returns(consulta);

        var resposta = await _sut.AnularAsync(10, "Duplicada por engano", new UsuarioAtual("admin@clinica.com", EhAdministrador: true));

        resposta.Status.Should().Be("Anulada");
        resposta.MotivoAnulacao.Should().Be("Duplicada por engano");
    }

    [Fact]
    public async Task AdicionarAsync_ComPacienteInativo_DeveSerRecusada()
    {
        var pessoa = new Pessoa("Paciente Inativo", new Cpf("52998224725"), new DateTime(1990, 1, 1), new Telefone("62988880000"));
        var paciente = new Paciente(pessoa);
        paciente.Inativar();
        _pacienteRepositorio.ObterId(1).Returns(paciente);

        var act = () => _sut.AdicionarAsync(new ConsultaRequest(1, 2, "Consulta", null));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inativo*");
    }
}
