using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class MedicoAppServiceTests
{
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IPessoaRepositorio _pessoaRepositorio = Substitute.For<IPessoaRepositorio>();
    private readonly MedicoAppService _sut;

    public MedicoAppServiceTests()
    {
        _sut = new MedicoAppService(_medicoRepositorio, _pessoaRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoCrmJaExiste_DeveLancarDomainException()
    {
        var request = new MedicoRequest(
            IdPessoa: null,
            Nome: "Dr. Roberto",
            Cpf: "52998224725",
            DtNasc: new DateTime(1980, 1, 1),
            Telefone: "11988889999",
            Observacao: null,
            Crm: "12345-SP",
            Especialidades: [Especialidade.Cardiologista]
        );

        _medicoRepositorio.ExisteCrm(Arg.Any<string>()).Returns(true);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Já existe um médico cadastrado com o CRM*");
    }

    [Fact]
    public async Task AdicionarAsync_ComDadosValidos_DeveCadastrarComSucesso()
    {
        var request = new MedicoRequest(
            IdPessoa: null,
            Nome: "Dr. Roberto",
            Cpf: "52998224725",
            DtNasc: new DateTime(1980, 1, 1),
            Telefone: "11988889999",
            Observacao: null,
            Crm: "12345-SP",
            Especialidades: [Especialidade.Cardiologista]
        );

        _medicoRepositorio.ExisteCrm(Arg.Any<string>()).Returns(false);
        _pessoaRepositorio.ExisteCpf(Arg.Any<string>()).Returns(false);
        _pessoaRepositorio.Adicionar(Arg.Any<Pessoa>()).Returns(call => call.Arg<Pessoa>());
        _medicoRepositorio.Adicionar(Arg.Any<Medico>()).Returns(call => call.Arg<Medico>());

        var response = await _sut.AdicionarAsync(request);

        response.Nome.Should().Be("Dr. Roberto");
        response.Crm.Should().Be("12345-SP");
        response.Especialidades.Should().Contain(Especialidade.Cardiologista);
    }
}
