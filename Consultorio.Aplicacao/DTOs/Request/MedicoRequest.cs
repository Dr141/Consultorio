using System.ComponentModel.DataAnnotations;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Request;

public record MedicoRequest(
    int? IdPessoa,
    string? Nome,
    string? Cpf,
    DateTime? DtNasc,
    string? Telefone,
    string? Observacao,

    [Required(ErrorMessage = "CRM é obrigatório")]
    string Crm,

    [Required(ErrorMessage = "Ao menos uma especialidade é obrigatória")]
    List<Especialidade> Especialidades,

    [EmailAddress(ErrorMessage = "E-mail de acesso inválido")]
    [StringLength(256, ErrorMessage = "E-mail de acesso muito longo")]
    string? EmailUsuario = null
);
