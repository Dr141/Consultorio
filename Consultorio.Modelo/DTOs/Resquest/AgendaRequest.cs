using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record AgendaRequest(DateTime data, TimeSpan hora, int? idMedico, int? idPaciente, int? idExame, int? idConsulta)
{
    [Required]
    public DateTime Data { get; init; } = data;
    [Required]
    public TimeSpan Hora { get; init; } = hora;
    public int? IdMedico { get; init; } = idMedico;
    public int? IdPaciente { get; init; } = idPaciente;
    public int? IdExame { get; init; } = idExame;
    public int? IdConsulta { get; init; } = idConsulta;
}
