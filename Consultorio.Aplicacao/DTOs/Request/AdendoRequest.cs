using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record AdendoRequest(
    [Required(ErrorMessage = "Informe o texto do adendo.")]
    [StringLength(2000, MinimumLength = 2, ErrorMessage = "O adendo deve ter entre 2 e 2000 caracteres.")]
    string Texto
);
