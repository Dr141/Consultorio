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
    int ExamesCriados = 0,
    bool Assinado = false,
    DateTime? DtAssinatura = null,
    string? AssinadoPor = null
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
    string NomeClinica,
    bool AssinadoDigitalmente = false,
    string? AssinadoPor = null,
    DateTime? DtAssinatura = null
);

/// <summary>Certificado digital A1 do médico (somente dados públicos; o arquivo e a senha nunca saem do servidor).</summary>
public record CertificadoDigitalResponse(
    int Id,
    int IdMedico,
    string Titular,
    string Emissor,
    string NumeroSerie,
    DateTime ValidoDe,
    DateTime ValidoAte,
    bool IcpBrasil,
    bool Vencido,
    int DiasParaVencer,
    string EnviadoPor,
    DateTime DtEnvio
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
