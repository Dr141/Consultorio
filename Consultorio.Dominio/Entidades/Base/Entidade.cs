namespace Consultorio.Dominio.Entidades.Base;

public abstract class Entidade
{
    public int Id { get; protected set; }
    public DateTime DtCriacao { get; protected set; }

    protected Entidade()
    {
        DtCriacao = DateTime.UtcNow;
    }
}
