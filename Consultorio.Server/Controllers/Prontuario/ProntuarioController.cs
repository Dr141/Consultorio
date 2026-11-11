using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Prontuario;

[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Clinico)]
[Auditar("Prontuario")]
[ApiController]
public class ProntuarioController(IProntuarioAppService prontuarioAppService) : ControllerBase
{
    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Gerar relatório de prontuário do paciente em formato de ofício com filtros")]
    [ProducesResponseType(typeof(ProntuarioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProntuarioResponse>> ObterProntuario(
        int idPaciente,
        [FromQuery] int? idMedico = null,
        [FromQuery] DateTime? dataInicio = null,
        [FromQuery] DateTime? dataFim = null)
    {
        try
        {
            var filtro = new ProntuarioFiltroRequest(idPaciente, idMedico, dataInicio, dataFim);
            var prontuario = await prontuarioAppService.GerarProntuarioAsync(filtro);
            return Ok(prontuario);
        }
        catch (DomainException ex)
        {
            if (ex.Message.Contains("não encontrad", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { message = ex.Message });
            }

            return BadRequest(new { message = ex.Message });
        }
    }
}
