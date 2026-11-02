namespace Consultorio.Aplicacao.DTOs.Response;

public record PessoaResponse(
    int Id,
    string Nome,
    string Cpf,
    DateTime DtNasc,
    string Telefone,
    string? Observacao,
    DateTime DtCriacao
);
