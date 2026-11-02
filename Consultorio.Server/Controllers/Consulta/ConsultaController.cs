using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Consulta;

[Route("api/[controller]")]
[Authorize]
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
    [EndpointSummary("Atualizar observações/evolução da consulta")]
    [ProducesResponseType(typeof(ConsultaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultaResponse>> AtualizarObservacao(int id, [FromBody] string observacao)
    {
        try
        {
            var atualizada = await consultaAppService.AtualizarObservacaoAsync(id, observacao);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Excluir consulta")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
}
