using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record ExameRequest
{
    public ExameRequest(string descricao, string? resultado, int idPaciente)
    {
        Descricao = descricao;
        Resultado = resultado;
        IdPaciente = idPaciente;
    }

    [Required]
    [Length(1, 100, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public string Descricao { get; init; }
    public string? Resultado { get; init; }
    [Required]
    public int IdPaciente { get; init; }
}
