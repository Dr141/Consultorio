namespace Consultorio.Aplicacao.DTOs.Response;

public record PacienteResponse(
    int Id,
    int IdPessoa,
    string Nome,
    string Cpf,
    DateTime DtNasc,
    string Telefone,
    string? Observacao,
    DateTime DtCriacao
);
