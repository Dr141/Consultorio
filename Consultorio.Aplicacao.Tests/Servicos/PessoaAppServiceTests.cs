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

public class PessoaAppServiceTests
{
    private readonly IPessoaRepositorio _pessoaRepositorio = Substitute.For<IPessoaRepositorio>();
    private readonly PessoaAppService _sut;

    public PessoaAppServiceTests()
    {
        _sut = new PessoaAppService(_pessoaRepositorio);
    }

    [Fact]
    public async Task AdicionarAsync_QuandoCpfNaoExiste_DeveCadastrarERetornarResponse()
    {
        var request = new PessoaRequest("Carlos Alberto", "52998224725", new DateTime(1985, 3, 20), "11987654321", "Obs");
        _pessoaRepositorio.ExisteCpf(Arg.Any<string>()).Returns(false);
        _pessoaRepositorio.Adicionar(Arg.Any<Pessoa>()).Returns(call => call.Arg<Pessoa>());

        var response = await _sut.AdicionarAsync(request);

        response.Nome.Should().Be("Carlos Alberto");
        response.Cpf.Should().Be("529.982.247-25");
        await _pessoaRepositorio.Received(1).Adicionar(Arg.Any<Pessoa>());
    }

    [Fact]
    public async Task AdicionarAsync_QuandoCpfJaExiste_DeveLancarDomainException()
    {
        var request = new PessoaRequest("Carlos Alberto", "52998224725", new DateTime(1985, 3, 20), "11987654321", null);
        _pessoaRepositorio.ExisteCpf(Arg.Any<string>()).Returns(true);

        var act = () => _sut.AdicionarAsync(request);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Já existe uma pessoa cadastrada com o CPF*");
    }

    [Fact]
    public async Task ObterPorIdAsync_QuandoExiste_DeveRetornarResponse()
    {
        var pessoa = new Pessoa("Ana Paula", new Cpf("52998224725"), new DateTime(1995, 1, 1), new Telefone("11988887777"));
        _pessoaRepositorio.ObterId(1).Returns(pessoa);

        var response = await _sut.ObterPorIdAsync(1);

        response.Should().NotBeNull();
        response!.Nome.Should().Be("Ana Paula");
    }
}
