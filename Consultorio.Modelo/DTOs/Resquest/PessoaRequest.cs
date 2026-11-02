using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record PessoaRequest
{
    public PessoaRequest(DateTime dtNasc, string cpf, string nome, string telefone, string? observacao)
    {
        DtNasc = dtNasc;
        Cpf = cpf;
        Nome = nome;
        Telefone = telefone;
        Observacao = observacao;
    }

    [Required]
    public DateTime DtNasc { get; init; }
    [Required]
    [Length(11, 11, ErrorMessage = "O campo {0} deve ter {1} caracteres")]
    public string Cpf { get; init; }
    [Required]
    [Length(1, 100, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public string Nome { get; init; }
    [Required]
    [Length(10, 11, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public string Telefone { get; init; }
    public string? Observacao { get; init; }
}
