namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuarioCadastroResponse
{
    public UsuarioCadastroResponse(bool sucesso, IEnumerable<string> erros)
    {
        Sucesso = sucesso;
        Erros = erros;
    }

    public bool Sucesso { get; set; }

    public IEnumerable<string> Erros { get; set; }
}