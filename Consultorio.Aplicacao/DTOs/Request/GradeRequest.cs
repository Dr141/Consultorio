using System.ComponentModel.DataAnnotations;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Request;

/// <summary>Gera horários em lote. DiasSemana: 0 = domingo ... 6 = sábado.</summary>
public record GradeRequest(
    [Required(ErrorMessage = "Selecione o médico")]
    int IdMedico,
    [Required] DateTime DataInicio,
    [Required] DateTime DataFim,
    [Required] List<int> DiasSemana,
    [Required] TimeSpan HoraInicio,
    [Required] TimeSpan HoraFim,
    [Range(5, 240, ErrorMessage = "O intervalo deve ficar entre 5 e 240 minutos")]
    int IntervaloMinutos,
    TimeSpan? PausaInicio = null,
    TimeSpan? PausaFim = null,
    TipoAtendimento? Tipo = null
);

public record BloqueioRequest(
    int? IdMedico,
    [Required] DateTime DataInicio,
    [Required] DateTime DataFim,
    [Required(ErrorMessage = "Informe o motivo")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "O motivo deve ter entre 3 e 200 caracteres")]
    string Motivo
);
