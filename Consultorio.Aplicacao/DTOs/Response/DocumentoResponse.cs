namespace Consultorio.Aplicacao.DTOs.Response;

public record DocumentoResponse(
    int Id,
    string Tipo,
    string TipoNome,
    int IdPaciente,
    string NomePaciente,
    string? CpfPaciente,
    DateTime? DtNascPaciente,
    int IdMedico,
    string NomeMedico,
    string CrmMedico,
    List<string> EspecialidadesMedico,
    int? IdConsulta,
    string Conteudo,
    int? DiasAfastamento,
    string? CidCodigo,
    string? CidDescricao,
    string CodigoVerificacao,
    string UrlVerificacao,
    DateTime DtEmissao,
    string EmitidoPor,
    bool Cancelado,
    DateTime? DtCancelamento,
    string? MotivoCancelamento,
    string NomeClinica,
    string? EnderecoClinica,
    string? TelefoneClinica,
    int ExamesCriados = 0
);

/// <summary>Dados mínimos exibidos na verificação pública (sem expor o conteúdo clínico).</summary>
public record VerificacaoDocumentoResponse(
    bool Encontrado,
    string? TipoNome,
    DateTime? DtEmissao,
    string? NomeMedico,
    string? CrmMedico,
    string? IniciaisPaciente,
    int? DiasAfastamento,
    bool Cancelado,
    string NomeClinica
);

public record CidResponse(string Codigo, string Descricao);

public record SinaisVitaisResponse(
    int? PressaoSistolica,
    int? PressaoDiastolica,
    int? FrequenciaCardiaca,
    int? FrequenciaRespiratoria,
    decimal? Temperatura,
    int? SaturacaoO2,
    decimal? PesoKg,
    int? AlturaCm,
    int? Glicemia,
    decimal? Imc
);

public record ArquivoResponse(string NomeArquivo, string ContentType, byte[] Conteudo);

public record ExameAnexoResponse(int Id, int IdExame, string NomeArquivo, string ContentType, long TamanhoBytes, string EnviadoPor, DateTime DtCriacao);
