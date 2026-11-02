using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Consultorio.Identity.Modelo.Configuracao;

/// <summary>
/// Classe base das configurações do Jwt.
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string SecurityKey { get; set; } = string.Empty;
    public int AccessTokenExpiration { get; set; } = 1;
    public int RefreshTokenExpiration { get; set; } = 7;
    public SigningCredentials SigningCredentials =>
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(string.IsNullOrEmpty(SecurityKey) ? "DefaultSecretKey12345678901234567890" : SecurityKey)), SecurityAlgorithms.HmacSha512);
}
