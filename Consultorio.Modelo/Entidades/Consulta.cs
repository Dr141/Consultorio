using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Consulta : Entidade
{
    public string Observacao { get; set; }
    public Paciente Paciente { get; set; }
    public int IdPaciente { get; set; }
    public Medico Medico { get; set; }
    public int IdMedico { get; set; }
}
