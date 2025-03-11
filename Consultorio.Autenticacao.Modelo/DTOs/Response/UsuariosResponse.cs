using Consultorio.Identity.Modelo.DTOs.Response.Base;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuariosResponse : ResponseBase
{
    public UsuariosResponse(bool sucesso, IEnumerable<UsuarioDto>? usuarios) : base(sucesso)
    {
        Usuarios = usuarios;
    }

    public IEnumerable<UsuarioDto>?  Usuarios { get; set; }
}