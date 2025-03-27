using Consultorio.Modelo.Entidades.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consultorio.Modelo.Entidades;

public class Consulta : Entidade
{
    public required string Observacao { get; init; }
    public Paciente? Paciente { get; set; }
    [ForeignKey("IdPaciente")]
    public int IdPaciente { get; init; }
    public Medico? Medico { get; set; }
    [ForeignKey("IdMedico")]
    public int IdMedico { get; init; }
}
