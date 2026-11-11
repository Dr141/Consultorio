using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Exame;

[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Recepcao)]
[Auditar("Exame")]
[ApiController]
public class ExameController(IExameAppService exameAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todos os exames")]
    [ProducesResponseType(typeof(List<ExameResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExameResponse>>> ObterTodos()
    {
        return Ok(await exameAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter exame por id")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExameResponse>> ObterPorId(int id)
    {
        var exame = await exameAppService.ObterPorIdAsync(id);
        if (exame is null) return NotFound(new { message = $"Exame com id {id} não encontrado." });
        return Ok(exame);
    }

    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Obter exames por paciente")]
    [ProducesResponseType(typeof(List<ExameResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExameResponse>>> ObterPorPaciente(int idPaciente)
    {
        return Ok(await exameAppService.ObterPorPacienteAsync(idPaciente));
    }

    [HttpPost]
    [EndpointSummary("Cadastrar novo exame")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> Adicionar([FromBody] ExameRequest request)
    {
        try
        {
            var criado = await exameAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Clinico)]
    [EndpointSummary("Atualizar exame")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> Atualizar(int id, [FromBody] ExameRequest request)
    {
        try
        {
            var atualizado = await exameAppService.AtualizarAsync(id, request);
            return Ok(atualizado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    public record RegistrarResultadoDto(string Resultado, int? IdMedico = null);

    [HttpPatch("{id:int}/resultado")]
    [Authorize(Policy = PoliticasAcesso.Clinico)]
    [EndpointSummary("Registrar resultado do exame")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> RegistrarResultado(int id, [FromBody] RegistrarResultadoDto dto)
    {
        try
        {
            var atualizado = await exameAppService.RegistrarResultadoAsync(id, dto.Resultado, dto.IdMedico);
            return Ok(atualizado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/cancelar")]
    [EndpointSummary("Cancelar solicitação de exame (o registro é preservado com o motivo)")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> Cancelar(int id, [FromBody] MotivoRequest request)
    {
        try
        {
            return Ok(await exameAppService.CancelarAsync(id, request.Motivo));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Exclusão não permitida: use cancelar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await exameAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
