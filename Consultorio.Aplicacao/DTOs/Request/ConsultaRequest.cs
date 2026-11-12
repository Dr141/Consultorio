using System.ComponentModel.DataAnnotations;

namespace Consultorio.Aplicacao.DTOs.Request;

/// <summary>
/// Registro de consulta. A evolução pode ser só o texto livre (Observacao) ou estruturada em SOAP;
/// ao menos um dos campos deve ser preenchido (validado no domínio).
/// </summary>
public record ConsultaRequest(
    [Required(ErrorMessage = "Id do Paciente é obrigatório")]
    int IdPaciente,

    [Required(ErrorMessage = "Id do Médico é obrigatório")]
    int IdMedico,

    [StringLength(2000, ErrorMessage = "Observação deve ter no máximo 2000 caracteres")]
    string? Observacao,

    int? IdAgenda,

    [StringLength(4000)] string? Subjetivo = null,
    [StringLength(4000)] string? Objetivo = null,
    [StringLength(4000)] string? Avaliacao = null,
    [StringLength(4000)] string? Plano = null,
    [StringLength(10)] string? CidCodigo = null,
    [StringLength(200)] string? CidDescricao = null,
    SinaisVitaisDto? SinaisVitais = null,
    DateTime? RetornoEm = null
);

/// <summary>Atualização da evolução de uma consulta ainda aberta.</summary>
public record EvolucaoRequest(
    [StringLength(2000)] string? Observacao,
    [StringLength(4000)] string? Subjetivo = null,
    [StringLength(4000)] string? Objetivo = null,
    [StringLength(4000)] string? Avaliacao = null,
    [StringLength(4000)] string? Plano = null,
    [StringLength(10)] string? CidCodigo = null,
    [StringLength(200)] string? CidDescricao = null,
    SinaisVitaisDto? SinaisVitais = null,
    DateTime? RetornoEm = null
);

public record SinaisVitaisDto(
    int? PressaoSistolica = null,
    int? PressaoDiastolica = null,
    int? FrequenciaCardiaca = null,
    int? FrequenciaRespiratoria = null,
    decimal? Temperatura = null,
    int? SaturacaoO2 = null,
    decimal? PesoKg = null,
    int? AlturaCm = null,
    int? Glicemia = null,
    decimal? Imc = null
);

public record AlertasClinicosRequest(
    [StringLength(2000)] string? Alergias,
    [StringLength(2000)] string? MedicamentosEmUso,
    [StringLength(2000)] string? CondicoesCronicas
);
