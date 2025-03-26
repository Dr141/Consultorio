using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Paciente : Entidade
{
    public Pessoa Pessoa { get; set; }
    public int IdPessoa { get; set; }
    public ICollection<Exame> Exames { get; set; }
    public ICollection<Consulta> Consultas { get; set; }
}
