namespace Consultorio.Aplicacao.DTOs.Response;

public record ExameResponse(
    int Id,
    int IdPaciente,
    string? NomePaciente,
    string Descricao,
    string? Resultado,
    int? IdAgenda,
    DateTime? DataHora,
    DateTime DtCriacao
);
