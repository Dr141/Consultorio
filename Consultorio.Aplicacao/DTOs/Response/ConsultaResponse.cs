namespace Consultorio.Aplicacao.DTOs.Response;

public record ConsultaResponse(
    int Id,
    int IdPaciente,
    string? NomePaciente,
    int IdMedico,
    string? NomeMedico,
    string Observacao,
    int? IdAgenda,
    DateTime? DataHora,
    DateTime DtCriacao
);
