using Consultorio.Aplicacao.DTOs.Portal;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Net.Http.Headers;

namespace Consultorio.Server.Controllers.Portal;

/// <summary>
/// Portal do paciente. Público para entrar (código por e-mail); demais rotas exigem a sessão do portal.
/// Todo acesso autenticado fica na trilha de auditoria como "portal:paciente:{id}".
/// </summary>
[Route("api/portal")]
[AllowAnonymous]
[ApiController]
public class PortalController(IPortalPacienteAppService portal) : ControllerBase
{
    private int IdPaciente => HttpContext.ObterIdPacientePortal();

    // ---------- Entrada ----------

    [HttpPost("acesso/codigo")]
    [EnableRateLimiting(PoliticaLimitePortal.Nome)]
    [EndpointSummary("Solicitar código de acesso (enviado ao e-mail cadastrado)")]
    [ProducesResponseType(typeof(PortalSolicitacaoResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortalSolicitacaoResponse>> SolicitarCodigo([FromBody] PortalSolicitarCodigoRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await portal.SolicitarCodigoAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), cancellationToken));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("acesso/sessao")]
    [EnableRateLimiting(PoliticaLimitePortal.Nome)]
    [EndpointSummary("Validar o código e abrir a sessão do portal")]
    [ProducesResponseType(typeof(PortalSessaoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortalSessaoResponse>> ValidarCodigo([FromBody] PortalValidarCodigoRequest request)
    {
        try
        {
            return Ok(await portal.ValidarCodigoAsync(request));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("acesso/sessao")]
    [EndpointSummary("Sair do portal")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Sair()
    {
        await portal.EncerrarSessaoAsync(Request.Headers[PortalAutenticacaoFilter.Cabecalho].ToString());
        return NoContent();
    }

    // ---------- Área do paciente ----------

    [HttpGet("resumo")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(PortalResumoResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<PortalResumoResponse>> Resumo() => Executar(() => portal.ObterResumoAsync(IdPaciente));

    [HttpGet("horarios")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(List<PortalMedicoHorariosResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<PortalMedicoHorariosResponse>>> Horarios() => Executar(() => portal.ObterHorariosDisponiveisAsync(IdPaciente));

    [HttpPost("agendamentos/{idAgenda:int}")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(PortalAgendamentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<ActionResult<PortalAgendamentoResponse>> Agendar(int idAgenda) => Executar(() => portal.AgendarAsync(IdPaciente, idAgenda));

    [HttpPatch("agendamentos/{idAgenda:int}/confirmar")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(PortalAgendamentoResponse), StatusCodes.Status200OK)]
    public Task<ActionResult<PortalAgendamentoResponse>> Confirmar(int idAgenda) => Executar(() => portal.ConfirmarAsync(IdPaciente, idAgenda));

    [HttpDelete("agendamentos/{idAgenda:int}")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Cancelar(int idAgenda)
    {
        try
        {
            await portal.CancelarAsync(IdPaciente, idAgenda);
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("exames")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(List<PortalExameResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<PortalExameResponse>>> Exames() => Executar(() => portal.ObterExamesAsync(IdPaciente));

    [HttpGet("exames/{idExame:int}/anexos/{idAnexo:int}")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArquivoExame(int idExame, int idAnexo)
    {
        var arquivo = await portal.ObterArquivoExameAsync(IdPaciente, idExame, idAnexo);
        if (arquivo is null) return NotFound(new { message = "Arquivo não encontrado." });
        var disposicao = new ContentDispositionHeaderValue("inline");
        disposicao.SetHttpFileName(arquivo.NomeArquivo);
        Response.Headers[HeaderNames.ContentDisposition] = disposicao.ToString();
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(arquivo.Conteudo, arquivo.ContentType);
    }

    [HttpGet("documentos")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(List<DocumentoResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<DocumentoResponse>>> Documentos() => Executar(() => portal.ObterDocumentosAsync(IdPaciente));

    [HttpGet("documentos/{id:int}")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(DocumentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoResponse>> Documento(int id)
    {
        var documento = await portal.ObterDocumentoAsync(IdPaciente, id);
        return documento is null ? NotFound(new { message = "Documento não encontrado." }) : Ok(documento);
    }

    [HttpGet("documentos/{id:int}/pdf")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DocumentoPdf(int id, [FromServices] IAssinaturaDigitalAppService assinatura)
    {
        var arquivo = await assinatura.ObterPdfPortalAsync(IdPaciente, id);
        if (arquivo is null) return NotFound(new { message = "Documento não encontrado." });
        var disposicao = new ContentDispositionHeaderValue("attachment");
        disposicao.SetHttpFileName(arquivo.NomeArquivo);
        Response.Headers[HeaderNames.ContentDisposition] = disposicao.ToString();
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(arquivo.Conteudo, arquivo.ContentType);
    }

    [HttpGet("cobrancas")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(List<CobrancaResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<List<CobrancaResponse>>> Cobrancas() => Executar(() => portal.ObterCobrancasAsync(IdPaciente));

    [HttpGet("cobrancas/{id:int}/recibo")]
    [PortalAutenticado, Auditar("Portal")]
    [ProducesResponseType(typeof(ReciboResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReciboResponse>> Recibo(int id)
    {
        var recibo = await portal.ObterReciboAsync(IdPaciente, id);
        return recibo is null ? NotFound(new { message = "Recibo não encontrado." }) : Ok(recibo);
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
