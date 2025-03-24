using System.Text.Json.Serialization;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuarioLoginResponse(string AccessToken, string RefreshToken);