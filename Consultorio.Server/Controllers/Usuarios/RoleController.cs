using Consultorio.Identity.Modelo.DTOs.Response;
using Consultorio.Identity.Modelo.DTOs.Resquest;
using Consultorio.Identity.Modelo.Enumerados;
using Consultorio.Identity.Modelo.Interfaces.Servicos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Consultorio.Server.Controllers.Usuarios;

[Route("[controller]")]
[ApiController]
[Authorize(Roles = nameof(Roles.Administrador))]
public class RoleController : ControllerBase
{
    private readonly IIdentityService _identity;

    public RoleController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("Adicionar")]
    [EndpointDescription("Método para adicionar role.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost(Name = "AdicionarRole")]
    public async Task<ActionResult<bool>> Adicionar(UsuarioRoleRequest adicionarRole)
    {
        try
        {
            await _identity.AdicionarRole(adicionarRole);
            return StatusCode(StatusCodes.Status202Accepted, true);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [EndpointSummary("Remover")]
    [EndpointDescription("Método para remover role.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete(Name = "RemoverRole")]
    public async Task<ActionResult<bool>> Remover(UsuarioRoleRequest removerRole)
    {
        try
        {
            await _identity.RemoverRole(removerRole);
            return StatusCode(StatusCodes.Status202Accepted, true);
        }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}
