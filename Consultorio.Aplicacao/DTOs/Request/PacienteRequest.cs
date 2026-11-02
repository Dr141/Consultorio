using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record PacienteRequest(
    int? IdPessoa,
    string? Nome,
    string? Cpf,
    DateTime? DtNasc,
    string? Telefone,
    string? Observacao
);
