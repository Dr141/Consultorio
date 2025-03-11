using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Usuarios;

[Route("[controller]")]
[ApiController]
[Authorize]
public class CadastroUsuarioController : ControllerBase
{
    private readonly IIdentityService _identity;

    public CadastroUsuarioController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("Cadastrar")]
    [EndpointDescription("Método para realizar Cadastro na API.")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [AllowAnonymous]
    [HttpPost(Name = "CadastroUsuario")]
    public async Task<ActionResult<UsuarioCadastroResponse>> Cadastrar(UsuarioCadastroRequest cadastro)
    {
        try
        {
            var result = await _identity.CadastrarUsuario(cadastro);
            return StatusCode(StatusCodes.Status201Created);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [EndpointSummary("AtualizarSenha")]
    [EndpointDescription("Método para atualizar senha do usuário autenticado na API.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPut(Name = "AtualizarSenha")]
    public async Task<ActionResult<UsuarioCadastroResponse>> AtualizarSenha(UsuarioAtualizarSenhaResquest atualizarSenha)
    {
        try
        {
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;

            if (atualizarSenha.SenhaAtual.Equals(atualizarSenha.NovaSenha, StringComparison.CurrentCultureIgnoreCase))
                return BadRequest("A nova senha deve ser diferente da senha atual.");
            var result = await _identity.AtualizarSenha(atualizarSenha, emailClaim);

            return StatusCode(StatusCodes.Status202Accepted);

        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}
