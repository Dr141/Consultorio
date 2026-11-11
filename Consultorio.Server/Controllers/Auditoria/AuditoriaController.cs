using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Identity.Modelo.Enumerados;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Auditoria;

[Route("api/[controller]")]
[Authorize(Roles = nameof(Roles.Administrador))]
[ApiController]
public class AuditoriaController(IAuditoriaAppService auditoriaAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Consultar trilha de auditoria (somente administradores)")]
    [ProducesResponseType(typeof(List<RegistroAuditoriaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RegistroAuditoriaResponse>>> Obter(
        [FromQuery] DateTime? inicio,
        [FromQuery] DateTime? fim,
        [FromQuery] string? usuario,
        [FromQuery] string? recurso,
        [FromQuery] string? idRecurso,
        [FromQuery] int limite = 500)
    {
        return Ok(await auditoriaAppService.ObterAsync(inicio, fim, usuario, recurso, idRecurso, limite));
    }
}
