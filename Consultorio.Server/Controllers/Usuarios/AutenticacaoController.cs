using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Usuarios;

[Route("api/[controller]")]
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
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    [HttpPost("Login")]
    public async Task<ActionResult<UsuarioLoginResponse>> Login(UsuarioLoginRequest usuario)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.Login(usuario);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("Cadastrar")]
    [EndpointDescription("Método para realizar Cadastro na API.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 201)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [AllowAnonymous]
    [HttpPost("Cadastro")]
    public async Task<ActionResult<UsuarioCadastroResponse>> Cadastrar(UsuarioCadastroRequest cadastro)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.CadastrarUsuario(cadastro);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("AtualizarSenha")]
    [EndpointDescription("Método para atualizar senha do usuário autenticado na API.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("AtualizarSenha")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AtualizarSenha(UsuarioAtualizarSenhaResquest atualizarSenha)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

                if (atualizarSenha.SenhaAtual.Equals(atualizarSenha.NovaSenha, StringComparison.CurrentCultureIgnoreCase))
                    return BadRequest("A nova senha deve ser diferente da senha atual.");
                var result = await _identity.AtualizarSenha(atualizarSenha, emailClaim);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("AtualizarToken")]
    [EndpointDescription("Método para atualizar token do usuário autenticado na API.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("AtualizarToken")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AtualizarToken()
    {
        try
        {
            if (ModelState.IsValid)
            {
                var usuarioId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (usuarioId == null)
                    return BadRequest("Realizar login novamente.");

                var result = await _identity.LoginSemSenha(usuarioId);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }
}
