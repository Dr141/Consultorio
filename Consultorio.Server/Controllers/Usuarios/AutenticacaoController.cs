using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace Consultorio.Server.Controllers.Usuarios;

[Route("[controller]")]
[Authorize]
[ApiController]
public class AutenticacaoController : ControllerBase
{
    private readonly IIdentityService _identity;

    public AutenticacaoController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("Login")]
    [EndpointDescription("Método para realizar autenticação na API.")]
    [ProducesResponseType(typeof(UsuarioLoginResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    [HttpPost(Name = "Login")]
    public async Task<ActionResult<UsuarioLoginResponse>> Login(UsuarioLoginRequest usuario)
    {
        try
        {
            var result = await _identity.Login(usuario);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [EndpointSummary("AtualizarToken")]
    [EndpointDescription("Método para atualizar token do usuário autenticado na API.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpGet(Name = "AtualizarToken")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AtualizarToken()
    {
        try
        {
            var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _identity.LoginSemSenha(usuarioId);
            return Ok(result);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}
