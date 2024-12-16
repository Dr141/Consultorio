namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuariosResponse
{
    public UsuariosResponse(bool sucesso, string erro, Dictionary<string, bool> usuarios)
    {
        Sucesso = sucesso;
        Erro = erro;
        Usuarios = usuarios;
    }

    public bool Sucesso { get; set; }

    public string Erro { get; set; }

    public Dictionary<string, bool> Usuarios { get; set; }
}