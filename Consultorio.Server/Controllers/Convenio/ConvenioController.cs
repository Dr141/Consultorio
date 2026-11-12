using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Convenio;

/// <summary>Convênios e tabela de preços. A equipe consulta; a gestão altera.</summary>
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Recepcao)]
[Auditar("Convenio")]
[ApiController]
public class ConvenioController(IConvenioAppService convenioAppService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Listar convênios")]
    [ProducesResponseType(typeof(List<ConvenioResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConvenioResponse>>> ObterTodos([FromQuery] bool incluirInativos = false) =>
        Ok(await convenioAppService.ObterTodosAsync(incluirInativos));

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter convênio")]
    [ProducesResponseType(typeof(ConvenioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ConvenioResponse>> ObterPorId(int id)
    {
        var convenio = await convenioAppService.ObterPorIdAsync(id);
        return convenio is null ? NotFound(new { message = $"Convênio com id {id} não encontrado." }) : Ok(convenio);
    }

    [HttpPost]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Cadastrar convênio")]
    [ProducesResponseType(typeof(ConvenioResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConvenioResponse>> Adicionar([FromBody] ConvenioRequest request)
    {
        try
        {
            var criado = await convenioAppService.AdicionarAsync(request);
            return CreatedAtAction(nameof(ObterPorId), new { id = criado.Id }, criado);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Atualizar convênio")]
    [ProducesResponseType(typeof(ConvenioResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ConvenioResponse>> Atualizar(int id, [FromBody] ConvenioRequest request) =>
        Executar(() => convenioAppService.AtualizarAsync(id, request));

    [HttpPatch("{id:int}/inativar")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Inativar convênio (pacientes vinculados mantêm o histórico)")]
    [ProducesResponseType(typeof(ConvenioResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<ConvenioResponse>> Inativar(int id) => Executar(() => convenioAppService.InativarAsync(id));

    [HttpPatch("{id:int}/reativar")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Reativar convênio")]
    [ProducesResponseType(typeof(ConvenioResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<ConvenioResponse>> Reativar(int id) => Executar(() => convenioAppService.ReativarAsync(id));

    [HttpGet("precos")]
    [EndpointSummary("Tabela de preços por tipo de atendimento (particular e por convênio)")]
    [ProducesResponseType(typeof(List<TabelaPrecoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TabelaPrecoResponse>>> ObterPrecos() => Ok(await convenioAppService.ObterPrecosAsync());

    [HttpPut("precos")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Definir o valor de um tipo de atendimento (cria ou atualiza)")]
    [ProducesResponseType(typeof(TabelaPrecoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<TabelaPrecoResponse>> DefinirPreco([FromBody] TabelaPrecoRequest request) =>
        Executar(() => convenioAppService.DefinirPrecoAsync(request));

    [HttpDelete("precos/{id:int}")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Remover um valor da tabela de preços")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RemoverPreco(int id)
    {
        try
        {
            await convenioAppService.RemoverPrecoAsync(id);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<ActionResult<T>> Executar<T>(Func<Task<T>> acao)
    {
        try
        {
            return Ok(await acao());
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
