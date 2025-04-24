using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record ExameRequest(string descricao, string? resultado, int idPaciente)
{
    [Required]
    [Length(1, 100, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public string Descricao { get; init; } = descricao;
    public string? Resultado { get; init; } = resultado;
    [Required]
    public int IdPaciente { get; init; } = idPaciente;
}
