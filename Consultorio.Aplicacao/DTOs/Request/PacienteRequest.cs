using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record PacienteRequest(
    int? IdPessoa,
    string? Nome,
    string? Cpf,
    DateTime? DtNasc,
    string? Telefone,
    string? Observacao,
    [EmailAddress(ErrorMessage = "E-mail inválido")]
    [StringLength(256, ErrorMessage = "E-mail muito longo")]
    string? Email = null
);
