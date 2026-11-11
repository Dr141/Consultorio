namespace Consultorio.Aplicacao.DTOs.Response;

public record ExameResponse(
    int Id,
    int IdPaciente,
    string? NomePaciente,
    string Descricao,
    string? Resultado,
    int? IdAgenda,
    DateTime? DataHora,
    DateTime DtCriacao,
    int? IdMedico = null,
    string? NomeMedico = null,
    string? CrmMedico = null,
    bool Cancelado = false,
    DateTime? DtCancelamento = null,
    string? MotivoCancelamento = null
);
