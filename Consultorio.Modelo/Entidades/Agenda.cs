using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Agenda : Entidade
{
    public required DateTime Data { get; init; }
    public required TimeSpan Hora { get; init; }
    public Medico? Medico { get; set; }
    public int? IdMedico { get; init; }
    public Paciente? Paciente { get; set; }
    public int? IdPaciente { get; init; }
    public Exame? Exame { get; set; }
    public int? IdExame { get; init; }
    public Consulta? Consulta { get; set; }
    public int? IdConsulta { get; init; }
}
