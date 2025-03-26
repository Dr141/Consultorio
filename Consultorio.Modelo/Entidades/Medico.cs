using Consultorio.Modelo.Entidades.Base;
using Consultorio.Modelo.Enumerados;

namespace Consultorio.Modelo.Entidades;

public class Medico : Entidade
{
    public Pessoa Pessoa { get; init; }
    public int IdPessoa { get; init; }
    public string Crm { get; init; }
    public IList<Especialidade> Especialidades { get; set; }
    public ICollection<Consulta> Consultas { get; set; }
}
