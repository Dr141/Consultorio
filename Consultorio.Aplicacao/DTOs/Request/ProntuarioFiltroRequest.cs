using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record ProntuarioFiltroRequest(
    [Required(ErrorMessage = "Id do Paciente é obrigatório.")]
    int IdPaciente,
    int? IdMedico = null,
    DateTime? DataInicio = null,
    DateTime? DataFim = null
);
