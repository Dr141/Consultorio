namespace Consultorio.Aplicacao.DTOs.Response;

public record AgendaResponse(
    int Id,
    DateTime Data,
    TimeSpan Hora,
    int? IdMedico,
    string? NomeMedico,
    int? IdPaciente,
    string? NomePaciente,
    bool Disponivel,
    int? IdConsulta,
    int? IdExame,
    DateTime DtCriacao
);
