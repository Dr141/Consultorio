using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Financeiro;

/// <summary>
/// Cobranças, recebimentos e recibos. A recepção lança e recebe; estornos e o resumo financeiro
/// ficam com a gestão.
/// </summary>
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Recepcao)]
[Auditar("Financeiro")]
[ApiController]
public class FinanceiroController(IFinanceiroAppService financeiroAppService) : ControllerBase
{
    [HttpGet("cobrancas")]
    [EndpointSummary("Cobranças lançadas no período (padrão: últimos 30 dias)")]
    [ProducesResponseType(typeof(List<CobrancaResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<CobrancaResponse>>> Obter(
        [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim, [FromQuery] string? status, [FromQuery] int? idPaciente) =>
        Executar(() => financeiroAppService.ObterAsync(inicio, fim, status, idPaciente));

    [HttpGet("cobrancas/pendentes")]
    [EndpointSummary("Todas as cobranças em aberto")]
    [ProducesResponseType(typeof(List<CobrancaResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CobrancaResponse>>> ObterPendentes() => Ok(await financeiroAppService.ObterPendentesAsync());

    [HttpGet("cobrancas/{id:int}")]
    [EndpointSummary("Obter cobrança")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CobrancaResponse>> ObterPorId(int id)
    {
        var cobranca = await financeiroAppService.ObterPorIdAsync(id);
        return cobranca is null ? NotFound(new { message = $"Cobrança com id {id} não encontrada." }) : Ok(cobranca);
    }

    [HttpPost("cobrancas")]
    [EndpointSummary("Lançar cobrança avulsa")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CobrancaResponse>> Criar([FromBody] CobrancaRequest request)
    {
        try
        {
            var criada = await financeiroAppService.CriarAsync(request, User.ObterUsuarioAtual());
            return CreatedAtAction(nameof(ObterPorId), new { id = criada.Id }, criada);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("atendimento/{idAgenda:int}")]
    [EndpointSummary("Gerar a cobrança de um atendimento pela tabela de preços")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<CobrancaResponse>> GerarDoAtendimento(int idAgenda) =>
        Executar(() => financeiroAppService.GerarDoAtendimentoAsync(idAgenda, User.ObterUsuarioAtual()));

    [HttpPatch("cobrancas/{id:int}/pagar")]
    [EndpointSummary("Registrar pagamento (com desconto opcional)")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<CobrancaResponse>> Pagar(int id, [FromBody] PagamentoRequest request) =>
        Executar(() => financeiroAppService.RegistrarPagamentoAsync(id, request, User.ObterUsuarioAtual()));

    [HttpPatch("cobrancas/{id:int}/cancelar")]
    [EndpointSummary("Cancelar cobrança pendente (com motivo)")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<CobrancaResponse>> Cancelar(int id, [FromBody] MotivoRequest request) =>
        Executar(() => financeiroAppService.CancelarAsync(id, request.Motivo));

    [HttpPatch("cobrancas/{id:int}/estornar")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Estornar cobrança paga (com motivo)")]
    [ProducesResponseType(typeof(CobrancaResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<CobrancaResponse>> Estornar(int id, [FromBody] MotivoRequest request) =>
        Executar(() => financeiroAppService.EstornarAsync(id, request.Motivo));

    [HttpGet("cobrancas/{id:int}/recibo")]
    [EndpointSummary("Dados do recibo de uma cobrança paga")]
    [ProducesResponseType(typeof(ReciboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<ReciboResponse>> Recibo(int id) => Executar(() => financeiroAppService.ObterReciboAsync(id));

    [HttpGet("resumo")]
    [Authorize(Policy = PoliticasAcesso.Gestao)]
    [EndpointSummary("Resumo financeiro do período: recebido, a receber, vencido, por forma e por convênio")]
    [ProducesResponseType(typeof(ResumoFinanceiroResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<ResumoFinanceiroResponse>> Resumo([FromQuery] DateTime inicio, [FromQuery] DateTime fim) =>
        Executar(() => financeiroAppService.ObterResumoAsync(inicio, fim));

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
