using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class PessoaTests
{
    private readonly Cpf _cpfValido = new("52998224725");
    private readonly Telefone _telefoneValido = new("11987654321");
    private readonly DateTime _dtNascValida = new(1990, 5, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CriarPessoa_ComDadosValidos_DeveInstanciarComSucesso()
    {
        var pessoa = new Pessoa("Maria da Silva", _cpfValido, _dtNascValida, _telefoneValido, "Observações gerais");

        pessoa.Nome.Should().Be("Maria da Silva");
        pessoa.Cpf.Valor.Should().Be("52998224725");
        pessoa.DtNasc.Should().Be(_dtNascValida);
        pessoa.Telefone.Valor.Should().Be("11987654321");
        pessoa.Observacao.Should().Be("Observações gerais");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("A")]
    public void CriarPessoa_ComNomeInvalido_DeveLancarDomainException(string nome)
    {
        var act = () => new Pessoa(nome, _cpfValido, _dtNascValida, _telefoneValido);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void CriarPessoa_ComDataNascimentoFutura_DeveLancarDomainException()
    {
        var act = () => new Pessoa("Maria da Silva", _cpfValido, DateTime.UtcNow.AddDays(1), _telefoneValido);

        act.Should().Throw<DomainException>()
            .WithMessage("*no futuro*");
    }

    [Fact]
    public void AtualizarPessoa_ComDadosValidos_DeveAtualizarPropriedades()
    {
        var pessoa = new Pessoa("Maria da Silva", _cpfValido, _dtNascValida, _telefoneValido);
        var novoTelefone = new Telefone("11911112222");

        pessoa.Atualizar("Maria dos Santos", _cpfValido, _dtNascValida, novoTelefone, "Nova observacao");

        pessoa.Nome.Should().Be("Maria dos Santos");
        pessoa.Telefone.Valor.Should().Be("11911112222");
        pessoa.Observacao.Should().Be("Nova observacao");
    }
}
