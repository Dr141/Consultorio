using Consultorio.Identity.Modelo.DTOs.Response.Base;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuarioCadastroResponse : ResponseBase
{
    public UsuarioCadastroResponse(bool sucesso) : base (sucesso)
    { }
}