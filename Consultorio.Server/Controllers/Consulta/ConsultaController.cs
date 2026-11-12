using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Consulta;

/// <summary>
/// Registros de consulta compõem o prontuário: só perfis clínicos acessam, todo acesso é auditado
/// e nenhum registro é apagado (consultas são finalizadas, complementadas por adendo ou anuladas).
/// </summary>
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Clinico)]
[Auditar("Consulta")]
[ApiController]
public class ConsultaController(IConsultaAppService consultaAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todas as consultas")]
    [ProducesResponseType(typeof(List<ConsultaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsultaResponse>>> ObterTodos()
    {
        return Ok(await consultaAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter consulta por id")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConsultaResponse>> ObterPorId(int id)
    {
        var consulta = await consultaAppService.ObterPorIdAsync(id);
        if (consulta is null) return NotFound(new { message = $"Consulta com id {id} não encontrada." });
        return Ok(consulta);
    }

    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Obter consultas por paciente")]
    [ProducesResponseType(typeof(List<ConsultaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsultaResponse>>> ObterPorPaciente(int idPaciente)
    {
        return Ok(await consultaAppService.ObterPorPacienteAsync(idPaciente));
    }

    [HttpGet("medico/{idMedico:int}")]
    [EndpointSummary("Obter consultas por médico")]
    [ProducesResponseType(typeof(List<ConsultaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsultaResponse>>> ObterPorMedico(int idMedico)
    {
        return Ok(await consultaAppService.ObterPorMedicoAsync(idMedico));
    }

    [HttpPost]
    [EndpointSummary("Cadastrar nova consulta")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> Adicionar([FromBody] ConsultaRequest request)
    {
        try
        {
            var criada = await consultaAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/observacao")]
    [EndpointSummary("Atualizar a evolução de uma consulta ainda aberta")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> AtualizarObservacao(int id, [FromBody] string observacao)
    {
        try
        {
            return Ok(await consultaAppService.AtualizarObservacaoAsync(id, observacao, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/finalizar")]
    [EndpointSummary("Finalizar a consulta (a evolução passa a ser imutável)")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> Finalizar(int id)
    {
        try
        {
            return Ok(await consultaAppService.FinalizarAsync(id, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/anular")]
    [EndpointSummary("Anular consulta registrada por engano (o registro permanece no histórico)")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> Anular(int id, [FromBody] MotivoRequest request)
    {
        try
        {
            return Ok(await consultaAppService.AnularAsync(id, request.Motivo, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/adendos")]
    [EndpointSummary("Adicionar adendo a uma consulta finalizada")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> AdicionarAdendo(int id, [FromBody] AdendoRequest request)
    {
        try
        {
            return Ok(await consultaAppService.AdicionarAdendoAsync(id, request.Texto, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Exclusão não permitida: use anular")]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await consultaAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/evolucao")]
    [EndpointSummary("Atualizar evolução estruturada (SOAP), CID e sinais vitais de uma consulta aberta")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> AtualizarEvolucao(int id, [FromBody] EvolucaoRequest request)
    {
        try
        {
            return Ok(await consultaAppService.AtualizarEvolucaoAsync(id, request, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
