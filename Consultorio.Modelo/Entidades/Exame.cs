using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Exame : Entidade
{
    public string Descricao { get; init; }
    public string Resultado { get; init; }
    public Paciente Paciente { get; set; }
    public int IdPaciente { get; init; }
}
