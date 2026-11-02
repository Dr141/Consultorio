using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Agenda;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class AgendaController(IAgendaAppService agendaAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todos os horários de agenda")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterTodos()
    {
        return Ok(await agendaAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter horário de agenda por id")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AgendaResponse>> ObterPorId(int id)
    {
        var agenda = await agendaAppService.ObterPorIdAsync(id);
        if (agenda is null) return NotFound(new { message = $"Horário de agenda com id {id} não encontrado." });
        return Ok(agenda);
    }

    [HttpGet("medico/{idMedico:int}")]
    [EndpointSummary("Obter agenda por médico e data")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterPorMedicoEData(int idMedico, [FromQuery] DateTime? data)
    {
        var dataFiltro = data ?? DateTime.UtcNow.Date;
        return Ok(await agendaAppService.ObterPorMedicoEDataAsync(idMedico, dataFiltro));
    }

    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Obter agenda por paciente")]
    [ProducesResponseType(typeof(List<AgendaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AgendaResponse>>> ObterPorPaciente(int idPaciente)
    {
        return Ok(await agendaAppService.ObterPorPacienteAsync(idPaciente));
    }

    [HttpPost]
    [EndpointSummary("Cadastrar novo horário de agenda")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> Adicionar([FromBody] AgendaRequest request)
    {
        try
        {
            var criada = await agendaAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Atualizar horário de agenda")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> Atualizar(int id, [FromBody] AgendaRequest request)
    {
        try
        {
            var atualizada = await agendaAppService.AtualizarAsync(id, request);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/agendar/{idPaciente:int}")]
    [EndpointSummary("Agendar paciente em horário")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> AgendarPaciente(int id, int idPaciente)
    {
        try
        {
            var atualizada = await agendaAppService.AgendarPacienteAsync(id, idPaciente);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/liberar")]
    [EndpointSummary("Liberar horário agendado")]
    [ProducesResponseType(typeof(AgendaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AgendaResponse>> LiberarHorario(int id)
    {
        try
        {
            var atualizada = await agendaAppService.LiberarHorarioAsync(id);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Excluir horário de agenda")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await agendaAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
