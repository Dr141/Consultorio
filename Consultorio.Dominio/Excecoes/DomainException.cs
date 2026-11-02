namespace Consultorio.Dominio.Excecoes;

public class DomainException : Exception
{
    public DomainException(string mensagem) : base(mensagem)
    {
    }

    public static void When(bool hasError, string mensagem)
    {
        if (hasError)
        {
            throw new DomainException(mensagem);
        }
    }
}
