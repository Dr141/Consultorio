using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Consultorio.Identity.Modelo.Configuracao;

/// <summary>
/// Classe base das configurações do Jwt.
/// </summary>
public class JwtOptions
{
    public string Issuer { get; set; }
    public string Audience { get; set; }
    public string SecurityKey { get; set; }
    public int AccessTokenExpiration { get; set; }
    public int RefreshTokenExpiration { get; set; }
    public SigningCredentials SigningCredentials { 
        get { return new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecurityKey)), SecurityAlgorithms.HmacSha512); } 
    }
}