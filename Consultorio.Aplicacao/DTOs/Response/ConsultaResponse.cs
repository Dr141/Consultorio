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
    DateTime DtCriacao,
    string Status = "Aberta",
    DateTime? DtFinalizacao = null,
    DateTime? DtAnulacao = null,
    string? MotivoAnulacao = null,
    List<ConsultaAdendoResponse>? Adendos = null
);
