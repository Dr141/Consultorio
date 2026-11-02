using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

public record ConsultaRequest(
    [Required(ErrorMessage = "Id do Paciente é obrigatório")]
    int IdPaciente,

    [Required(ErrorMessage = "Id do Médico é obrigatório")]
    int IdMedico,

    [Required(ErrorMessage = "Observação é obrigatória")]
    [StringLength(2000, MinimumLength = 1, ErrorMessage = "Observação deve ter entre 1 e 2000 caracteres")]
    string Observacao,

    int? IdAgenda
);
