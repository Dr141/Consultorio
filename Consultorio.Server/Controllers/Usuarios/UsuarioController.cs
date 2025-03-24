using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Enumerados;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Usuarios;

[Route("[controller]")]
[Authorize(Roles = nameof(Roles.Administrador))]
[ApiController]
public class UsuarioController : ControllerBase
{
    private readonly IIdentityService _identity;

    public UsuarioController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("ObterTodos")]
    [EndpointDescription("Método para obter todos usuários cadastrados.")]
    [ProducesResponseType(typeof(UsuariosResponse), StatusCodes.Status200OK)]
    [HttpGet(Name = "ObterUsuarios")]
    public async Task<ActionResult<UsuariosResponse>> ObterTodos()
    {
        var result = await _identity.ObterTodosUsuarios();
        return Ok(result);
    }

    [EndpointSummary("AtualizarSenha")]
    [EndpointDescription("Método para atualizar a senha de terceiros.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost(Name = "AtualizarSenhaInterno")]
    public async Task<ActionResult<bool>> AtualizarSenha(UsuarioCadastroRequest atualizarSenha)
    {
        try
        {
            var result = await _identity.AtualizarSenhaInterno(atualizarSenha);
            return StatusCode(StatusCodes.Status202Accepted,true);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}
