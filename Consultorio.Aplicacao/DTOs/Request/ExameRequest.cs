using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record ExameRequest(
    [Required(ErrorMessage = "Id do Paciente é obrigatório")]
    int IdPaciente,

    [Required(ErrorMessage = "Descrição é obrigatória")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Descrição deve ter entre 2 e 200 caracteres")]
    string Descricao,

    string? Resultado,
    int? IdAgenda,
    int? IdMedico = null
);
