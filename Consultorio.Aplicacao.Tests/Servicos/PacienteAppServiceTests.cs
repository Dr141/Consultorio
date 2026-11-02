using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class PacienteAppServiceTests
{
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IPessoaRepositorio _pessoaRepositorio = Substitute.For<IPessoaRepositorio>();
    private readonly PacienteAppService _sut;

    public PacienteAppServiceTests()
    {
        _sut = new PacienteAppService(_pacienteRepositorio, _pessoaRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_ComNovaPessoa_DeveCadastrarPessoaEPaciente()
    {
        var request = new PacienteRequest(
            IdPessoa: null,
            Nome: "Fernanda Costa",
            Cpf: "52998224725",
            DtNasc: new DateTime(1992, 7, 10),
            Telefone: "11977778888",
            Observacao: "Paciente sem restrições"
        );

        _pessoaRepositorio.ExisteCpf(Arg.Any<string>()).Returns(false);
        _pessoaRepositorio.Adicionar(Arg.Any<Pessoa>()).Returns(call => call.Arg<Pessoa>());
        _pacienteRepositorio.Adicionar(Arg.Any<Paciente>()).Returns(call => call.Arg<Paciente>());

        var response = await _sut.AdicionarAsync(request);

        response.Nome.Should().Be("Fernanda Costa");
        await _pessoaRepositorio.Received(1).Adicionar(Arg.Any<Pessoa>());
        await _pacienteRepositorio.Received(1).Adicionar(Arg.Any<Paciente>());
    }

    [Fact]
    public async Task AdicionarAsync_ComPessoaExistenteJaSendoPaciente_DeveLancarDomainException()
    {
        var pessoa = new Pessoa("Fernanda Costa", new Cpf("52998224725"), new DateTime(1992, 7, 10), new Telefone("11977778888"));
        _pessoaRepositorio.ObterId(1).Returns(pessoa);
        _pacienteRepositorio.ObterPorIdPessoa(1).Returns(new Paciente(pessoa));

        var request = new PacienteRequest(IdPessoa: 1, null, null, null, null, null);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*já está cadastrada como paciente*");
    }
}
