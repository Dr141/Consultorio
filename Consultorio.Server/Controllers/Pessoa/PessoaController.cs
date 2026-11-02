using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Pessoa;

[Route("api/[controller]")]
[Authorize]
[ApiController]
public class PessoaController(IPessoaAppService pessoaAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Obter todas as pessoas")]
    [ProducesResponseType(typeof(List<PessoaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PessoaResponse>>> ObterTodos()
    {
        return Ok(await pessoaAppService.ObterTodosAsync());
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter pessoa por id")]
    [ProducesResponseType(typeof(PessoaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PessoaResponse>> ObterPorId(int id)
    {
        var pessoa = await pessoaAppService.ObterPorIdAsync(id);
        if (pessoa is null) return NotFound(new { message = $"Pessoa com id {id} não encontrada." });
        return Ok(pessoa);
    }

    [HttpPost]
    [EndpointSummary("Cadastrar nova pessoa")]
    [ProducesResponseType(typeof(PessoaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PessoaResponse>> Adicionar([FromBody] PessoaRequest request)
    {
        try
        {
            var criada = await pessoaAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Atualizar pessoa")]
    [ProducesResponseType(typeof(PessoaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PessoaResponse>> Atualizar(int id, [FromBody] PessoaRequest request)
    {
        try
        {
            var atualizada = await pessoaAppService.AtualizarAsync(id, request);
            return Ok(atualizada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Excluir pessoa")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int id)
    {
        try
        {
            await pessoaAppService.RemoverAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
