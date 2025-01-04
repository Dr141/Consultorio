namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuariosResponse
{
    public UsuariosResponse(bool sucesso, string erro, IEnumerable<UsuarioDto>? usuarios)
    {
        Sucesso = sucesso;
        Erro = erro;
        Usuarios = usuarios;
    }

    public bool Sucesso { get; set; }
    public string Erro { get; set; }

    public IEnumerable<UsuarioDto>?  Usuarios { get; set; }
}