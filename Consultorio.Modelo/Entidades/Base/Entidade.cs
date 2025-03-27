using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.Entidades.Base;

public abstract class Entidade
{
    [Key]
    public int Id { get; init; }
    public DateTime DtCriacao { get; init; }

    protected Entidade() => DtCriacao = DateTime.Now;
}
