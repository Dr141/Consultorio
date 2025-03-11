using Consultorio.Identity.Modelo.DTOs.Response.Base;
using System.Text.Json.Serialization;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuarioLoginResponse : ResponseBase
{
    public UsuarioLoginResponse(bool sucesso, string accessToken, string refreshToken) : base(sucesso)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken;
    }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string AccessToken { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string RefreshToken { get; set; }
}