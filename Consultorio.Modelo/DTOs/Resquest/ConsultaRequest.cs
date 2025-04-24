using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record ConsultaRequest(int idPaciente, int idMedico, int idAgenda, string observacao)
{
    [Required]
    [Length(1, 100, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public string Observacao { get; init; } = observacao;
    [Required]
    public int IdPaciente { get; init; } = idPaciente;
    [Required]
    public int IdMedico { get; init; } = idMedico;
    [Required]
    public int IdAgenda { get; init; } = idAgenda;
}