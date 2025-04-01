using Consultorio.Identity.Modelo.Configuracao;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Consultorio.Server.Controllers.Usuarios;

[Route("[controller]")]
[Authorize]
[ApiController]
public class AutenticacaoController : ControllerBase
{
    private readonly IIdentityService _identity;
    private readonly JwtOptions _jwtOptions;
    public AutenticacaoController(IIdentityService identity, IOptions<JwtOptions> jwtOptions)
    {
        _identity = identity;
        _jwtOptions = jwtOptions.Value;
    }

    [EndpointSummary("Login")]
    [EndpointDescription("Método para realizar autenticação na API.")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    [HttpPost(Name = "Login")]
    public async Task<ActionResult<bool>> Login(UsuarioLoginRequest usuario)
    {
        try
        {
            var result = await _identity.Login(usuario);
            // Configuração do AccessToken (expira em 1 hora)
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(_jwtOptions.AccessTokenExpiration),
                Path = "/" // Disponível para toda a aplicação
            };

            // Configuração do RefreshToken (expira em 7 dias)
            var refreshTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiration),
                Path = "/"
            };

            // Configuração do roles (expira em 7 dias)
            var roles = new CookieOptions
            {
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiration),
                Path = "/"
            };

            // Armazena os cookies
            Response.Cookies.Append("AccessToken", result.AccessToken, accessTokenOptions);
            Response.Cookies.Append("RefreshToken", result.RefreshToken, refreshTokenOptions);
            if(result.Roles is not null)
                Response.Cookies.Append("Roles", string.Join("-", result.Roles), roles);

            return Ok(true);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [EndpointSummary("Refresh")]
    [EndpointDescription("Método para atualizar token do usuário autenticado na API.")]
    [ProducesResponseType(typeof(bool), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]    
    [HttpGet(Name = "Refresh")]
    public async Task<ActionResult<bool>> Refresh()
    {
        try
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(usuarioId))
                return Unauthorized(new { message = "Usuário não autenticado" });

            var newAuthen = await _identity.LoginSemSenha(usuarioId);
            var accessTokenOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddHours(_jwtOptions.AccessTokenExpiration), // Novo access token com 1 hora
                Path = "/"
            };

            Response.Cookies.Append("AccessToken", newAuthen.AccessToken, accessTokenOptions);
            return Ok(true);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [EndpointSummary("Logout")]
    [EndpointDescription("Método para realizar Logout.")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]       
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
    [HttpDelete(Name = "Logout")]
    public ActionResult<bool> Logout()
    {
        try
        {
            //por enquanto o token não sera salvo no db
            //var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
            //await _identity.Logout(emailClaim);

            Response.Cookies.Delete("AccessToken");
            Response.Cookies.Delete("RefreshToken");

            return Ok(true);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
