using System.Text.Json.Serialization;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuarioLoginResponse
{
    public UsuarioLoginResponse(bool sucesso, string accessToken, string refreshToken, IEnumerable<string> erros)
    {
        Sucesso = sucesso;
        AccessToken = accessToken;
        RefreshToken = refreshToken;
        Erros = erros;
    }

    public bool Sucesso { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string AccessToken { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string RefreshToken { get; set; }

    public IEnumerable<string> Erros { get; set; }
}