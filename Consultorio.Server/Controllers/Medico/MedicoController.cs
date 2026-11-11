using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Medico;

[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Recepcao)]
[ApiController]
public class MedicoController(IMedicoAppService medicoAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todos os médicos")]
    [ProducesResponseType(typeof(List<MedicoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MedicoResponse>>> ObterTodos([FromQuery] bool incluirInativos = false)
    {
        return Ok(await medicoAppService.ObterTodosAsync(incluirInativos));
    }

    [HttpGet("especialidades")]
    [EndpointSummary("Obter lista de especialidades")]
    [ProducesResponseType(typeof(List<object>), StatusCodes.Status200OK)]
    public ActionResult<List<object>> ObterEspecialidades()
    {
        var especialidades = Enum.GetValues<Especialidade>()
            .Select(e => new { Id = (int)e, Nome = e.ObterNome() })
            .ToList();

        return Ok(especialidades);
    }

    [HttpGet("meu")]
    [EndpointSummary("Médico vinculado ao usuário logado (204 se o usuário não for médico)")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<MedicoResponse>> ObterMeu()
    {
        var medico = await medicoAppService.ObterPorUsuarioAsync(User.ObterEmail());
        if (medico is null) return NoContent();
        return Ok(medico);
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter médico por id")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MedicoResponse>> ObterPorId(int id)
    {
        var medico = await medicoAppService.ObterPorIdAsync(id);
        if (medico is null) return NotFound(new { message = $"Médico com id {id} não encontrado." });
        return Ok(medico);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Cadastrar novo médico")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicoResponse>> Adicionar([FromBody] MedicoRequest request)
    {
        try
        {
            var criado = await medicoAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Atualizar médico")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicoResponse>> Atualizar(int id, [FromBody] MedicoRequest request)
    {
        try
        {
            var atualizado = await medicoAppService.AtualizarAsync(id, request);
            return Ok(atualizado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Inativar médico (o histórico é preservado)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await medicoAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/reativar")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Reativar médico inativo")]
    [ProducesResponseType(typeof(MedicoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicoResponse>> Reativar(int id)
    {
        try
        {
            return Ok(await medicoAppService.ReativarAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
