using System.ComponentModel.DataAnnotations;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Request;

public record AgendaRequest(
    [Required(ErrorMessage = "Data é obrigatória")]
    DateTime Data,

    [Required(ErrorMessage = "Hora é obrigatória")]
    TimeSpan Hora,

    int? IdMedico,
    int? IdPaciente,
    int? IdConsulta,
    int? IdExame,
    TipoAtendimento? Tipo = null,
    [Range(5, 480, ErrorMessage = "A duração deve ficar entre 5 e 480 minutos")]
    int? DuracaoMinutos = null
);
