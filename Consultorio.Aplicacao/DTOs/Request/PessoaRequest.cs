using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record PessoaRequest(
    [Required(ErrorMessage = "Nome é obrigatório")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 100 caracteres")]
    string Nome,

    [Required(ErrorMessage = "CPF é obrigatório")]
    string Cpf,

    [Required(ErrorMessage = "Data de nascimento é obrigatória")]
    DateTime DtNasc,

    [Required(ErrorMessage = "Telefone é obrigatório")]
    string Telefone,

    [StringLength(500, ErrorMessage = "Observação não pode exceder 500 caracteres")]
    string? Observacao
);
