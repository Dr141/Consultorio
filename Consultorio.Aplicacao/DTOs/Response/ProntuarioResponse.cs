namespace Consultorio.Aplicacao.DTOs.Response;

public record MedicoResumoProntuarioResponse(
    int Id,
    string Nome,
    string Crm,
    List<string> Especialidades
);

public record ConsultaProntuarioResponse(
    int Id,
    int IdMedico,
    string NomeMedico,
    string CrmMedico,
    List<string> EspecialidadesMedico,
    string Observacao,
    DateTime DataAtendimento,
    DateTime DtCriacao,
    string Status = "Aberta",
    string? MotivoAnulacao = null,
    List<ConsultaAdendoResponse>? Adendos = null,
    string? Subjetivo = null,
    string? Objetivo = null,
    string? Avaliacao = null,
    string? Plano = null,
    string? CidCodigo = null,
    string? CidDescricao = null,
    SinaisVitaisResponse? SinaisVitais = null
);

public record ExameProntuarioResponse(
    int Id,
    string Descricao,
    string? Resultado,
    bool PossuiLaudo,
    int? IdMedico,
    string? NomeMedico,
    string? CrmMedico,
    DateTime DataExame,
    DateTime DtCriacao,
    bool Cancelado = false,
    string? MotivoCancelamento = null,
    string Status = "Solicitado"
);

public record ProntuarioResumoResponse(
    int TotalConsultas,
    int TotalExames,
    int TotalLaudosEmitidos,
    int TotalExamesPendentes
);

public record ProntuarioResponse(
    string NumeroOficio,
    DateTime DataEmissao,
    PacienteResponse Paciente,
    MedicoResumoProntuarioResponse? MedicoFiltro,
    DateTime? DataInicio,
    DateTime? DataFim,
    ProntuarioResumoResponse Resumo,
    List<ConsultaProntuarioResponse> Consultas,
    List<ExameProntuarioResponse> Exames,
    List<DocumentoResponse>? Documentos = null
);
