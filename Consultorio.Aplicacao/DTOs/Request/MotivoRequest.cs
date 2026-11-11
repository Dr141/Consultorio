using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record MotivoRequest(
    [Required(ErrorMessage = "Informe o motivo.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "O motivo deve ter entre 5 e 500 caracteres.")]
    string Motivo
);
