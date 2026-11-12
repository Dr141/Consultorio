using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Indicadores;

[Route("api/[controller]")]
[ApiController]
public class IndicadoresController(IIndicadoresAppService indicadoresAppService) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Indicadores de gestão do período (agenda, exames, financeiro, diagnósticos)")]
    [ProducesResponseType(typeof(IndicadoresResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IndicadoresResponse>> Obter([FromQuery] DateTime inicio, [FromQuery] DateTime fim, [FromQuery] int? idMedico)
    {
        try
        {
            return Ok(await indicadoresAppService.ObterAsync(inicio, fim, idMedico));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("pendencias")]
    [Authorize(Policy = PoliticasAcesso.Recepcao)]
    [EndpointSummary("Pendências operacionais: exames atrasados, laudos não entregues, carteirinhas, cobranças vencidas, consultas abertas e retornos")]
    [ProducesResponseType(typeof(PendenciasResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PendenciasResponse>> Pendencias() => Ok(await indicadoresAppService.ObterPendenciasAsync());
}
