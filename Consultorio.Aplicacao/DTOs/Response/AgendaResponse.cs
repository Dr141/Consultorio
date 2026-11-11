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
    DateTime DtCriacao,
    string? Status = null,
    string Tipo = "Consulta",
    int DuracaoMinutos = 30,
    DateTime? DtConfirmacao = null,
    DateTime? DtChegada = null,
    DateTime? DtInicioAtendimento = null,
    DateTime? DtFimAtendimento = null,
    DateTime? LembreteEnviadoEm = null,
    string? TelefonePaciente = null,
    string? EmailPaciente = null
);
