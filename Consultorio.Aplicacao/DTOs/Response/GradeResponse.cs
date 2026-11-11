namespace Consultorio.Aplicacao.DTOs.Response;

public record GradeResponse(int Criados, int IgnoradosExistentes, int IgnoradosBloqueio);

public record BloqueioResponse(
    int Id,
    int? IdMedico,
    string? NomeMedico,
    DateTime DataInicio,
    DateTime DataFim,
    string Motivo,
    DateTime DtCriacao
);

/// <summary>
/// Resultado da criação de um bloqueio: horários livres do período são removidos;
/// os que já tinham paciente são devolvidos como conflitos para a recepção remarcar.
/// </summary>
public record BloqueioCriadoResponse(
    BloqueioResponse Bloqueio,
    int HorariosLivresRemovidos,
    List<AgendaResponse> Conflitos
);

public enum ResultadoConfirmacao
{
    Confirmado,
    JaConfirmado,
    Desmarcado,
    LinkInvalido,
    NaoPermitido
}

public record ConfirmacaoPacienteResponse(ResultadoConfirmacao Resultado, string Mensagem, AgendaResponse? Agenda);

public record ResultadoRotinaResponse(int Processados, int Enviados, int SemContato, int Falhas);
