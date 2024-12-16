using Microsoft.IdentityModel.Tokens;

namespace Consultorio.Identity.Modelo.Configuracao;

/// <summary>
/// Classe base das configurações do Jwt.
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public SigningCredentials SigningCredentials { get; set; }
    public int AccessTokenExpiration { get; set; }
    public int RefreshTokenExpiration { get; set; }
}