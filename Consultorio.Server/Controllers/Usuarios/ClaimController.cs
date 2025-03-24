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
public class ClaimController : ControllerBase
{
    private readonly IIdentityService _identity;

    public ClaimController(IIdentityService identity) => _identity = identity;

    [EndpointSummary("Adicionar")]
    [EndpointDescription("Método para adicionar claim.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpPost(Name = "AdicionarClaim")]
    public async Task<ActionResult<bool>> Adicionar(UsuarioClaimRequest adicionarClaim)
    {
        try
        {
            await _identity.AdicionarClaim(adicionarClaim);
            return Ok(true);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    [EndpointSummary("Remover")]
    [EndpointDescription("Método para remover claim.")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [HttpDelete(Name = "RemoverClaim")]
    public async Task<ActionResult<bool>> Remover(UsuarioClaimRequest removerClaim)
    {
        try
        {
            await _identity.RemoverClaim(removerClaim);
            return Ok(true);
        }
        catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
    }
}
