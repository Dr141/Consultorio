using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record AgendaRequest(
    [Required(ErrorMessage = "Data é obrigatória")]
    DateTime Data,

    [Required(ErrorMessage = "Hora é obrigatória")]
    TimeSpan Hora,

    int? IdMedico,
    int? IdPaciente,
    int? IdConsulta,
    int? IdExame
);
