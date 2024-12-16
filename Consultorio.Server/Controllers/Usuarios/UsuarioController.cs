using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Enumerados;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Usuarios;

[Route("api/[controller]")]
[Authorize(Roles = nameof(Roles.Administrador))]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly IIdentityService _identity;

    public UsuarioController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("ObterTodos")]
    [EndpointDescription("Método para obter todos usuários cadastrados.")]
    [ProducesResponseType(typeof(UsuariosResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpGet("ObterTodosUsuarios")]
    public async Task<ActionResult<UsuariosResponse>> ObterTodos()
    {
        var result = await _identity.ObterTodosUsuarios();
        return Ok(result);
    }

    [EndpointSummary("AtualizarSenha")]
    [EndpointDescription("Método para atualizar a senha de terceiros.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPut("AtualizarSenha")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AtualizarSenha(UsuarioCadastroRequest atualizarSenha)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.AtualizarSenhaInterno(atualizarSenha);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("AdicionarRole")]
    [EndpointDescription("Método para adicionar role.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("AdicionarRole")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AdicionarRole(UsuarioRoleRequest adicionarRole)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.AdicionarRole(adicionarRole);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("AdicionarRole")]
    [EndpointDescription("Método para remover role.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("RemoverRole")]
    public async Task<ActionResult<UsuarioCadastroResponse>> RemoverRole(UsuarioRoleRequest removerRole)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.RemoverRole(removerRole);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("AdicionarClaim")]
    [EndpointDescription("Método para adicionar claim.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpPost("AdicionarClaim")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AdicionarClaim(UsuarioClaimRequest adicionarClaim)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.AdicionarClaim(adicionarClaim);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }

    [EndpointSummary("RemoverClaim")]
    [EndpointDescription("Método para remover claim.")]
    [ProducesResponseType(typeof(UsuarioCadastroResponse), 200)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [HttpDelete("RemoverClaim")]
    public async Task<ActionResult<UsuarioCadastroResponse>> RemoverClaim(UsuarioClaimRequest removerClaim)
    {
        try
        {
            if (ModelState.IsValid)
            {
                var result = await _identity.RemoverClaim(removerClaim);

                if (result.Sucesso)
                    return Ok(result);

                return BadRequest(result.Erros);
            }

            return StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex) { return StatusCode(StatusCodes.Status500InternalServerError, ex.Message); }
    }
}
