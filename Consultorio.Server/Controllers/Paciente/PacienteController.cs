using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Paciente;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class PacienteController(IPacienteAppService pacienteAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todos os pacientes")]
    [ProducesResponseType(typeof(List<PacienteResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PacienteResponse>>> ObterTodos()
    {
        return Ok(await pacienteAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter paciente por id")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PacienteResponse>> ObterPorId(int id)
    {
        var paciente = await pacienteAppService.ObterPorIdAsync(id);
        if (paciente is null) return NotFound(new { message = $"Paciente com id {id} não encontrado." });
        return Ok(paciente);
    }

    [HttpPost]
    [EndpointSummary("Cadastrar novo paciente")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PacienteResponse>> Adicionar([FromBody] PacienteRequest request)
    {
        try
        {
            var criado = await pacienteAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Atualizar paciente")]
    [ProducesResponseType(typeof(PacienteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PacienteResponse>> Atualizar(int id, [FromBody] PacienteRequest request)
    {
        try
        {
            var atualizado = await pacienteAppService.AtualizarAsync(id, request);
            return Ok(atualizado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Excluir paciente")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await pacienteAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
