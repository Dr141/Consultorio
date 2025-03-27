using Consultorio.Modelo.Entidades.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace Consultorio.Modelo.Entidades;

public class Paciente : Entidade
{
    public Pessoa? Pessoa { get; set; }
    [ForeignKey("IdPessoa")]
    public required int IdPessoa { get; init; }
    public ICollection<Exame>? Exames { get; set; }
    public ICollection<Consulta>? Consultas { get; set; }
}
