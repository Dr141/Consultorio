using Consultorio.Modelo.Entidades.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consultorio.Modelo.Entidades;

public class Exame : Entidade
{
    public required string Descricao { get; init; }
    public required string Resultado { get; init; }
    public Paciente? Paciente { get; set; }
    [ForeignKey("IdPaciente")]
    public int IdPaciente { get; init; }
}
