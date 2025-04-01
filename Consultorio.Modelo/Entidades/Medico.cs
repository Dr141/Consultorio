using Consultorio.Modelo.Entidades.Base;
using Consultorio.Modelo.Enumerados;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consultorio.Modelo.Entidades;

public class Medico : Entidade
{
    public Pessoa? Pessoa { get; init; }
    [ForeignKey("IdPessoa")]
    public int IdPessoa { get; init; }
    public required string Crm { get; init; }
    public required IList<Especialidade> Especialidades { get; set; }
    public ICollection<Consulta>? Consultas { get; set; }
    public ICollection<Agenda>? Agendas { get; set; }
}
