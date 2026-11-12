using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Consultorio.Server.Controllers.Documento;

/// <summary>Receitas, atestados, declarações, pedidos de exames e encaminhamentos.</summary>
[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Clinico)]
[Auditar("Documento")]
[ApiController]
public class DocumentoController(IDocumentoAppService documentoAppService, IAssinaturaDigitalAppService assinaturaAppService) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Emitir documento clínico")]
    [ProducesResponseType(typeof(DocumentoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentoResponse>> Emitir([FromBody] DocumentoRequest request)
    {
        try
        {
            var emitido = await documentoAppService.EmitirAsync(request, User.ObterUsuarioAtual());
            return CreatedAtAction(nameof(ObterPorId), new { id = emitido.Id }, emitido);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    [EndpointSummary("Obter documento para visualização/impressão")]
    [ProducesResponseType(typeof(DocumentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DocumentoResponse>> ObterPorId(int id)
    {
        var documento = await documentoAppService.ObterPorIdAsync(id);
        if (documento is null) return NotFound(new { message = $"Documento com id {id} não encontrado." });
        return Ok(documento);
    }

    [HttpGet("paciente/{idPaciente:int}")]
    [EndpointSummary("Documentos emitidos para o paciente")]
    [ProducesResponseType(typeof(List<DocumentoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DocumentoResponse>>> ObterPorPaciente(int idPaciente)
    {
        return Ok(await documentoAppService.ObterPorPacienteAsync(idPaciente));
    }

    [HttpPatch("{id:int}/cancelar")]
    [EndpointSummary("Cancelar documento emitido por engano (continua no histórico com o motivo)")]
    [ProducesResponseType(typeof(DocumentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentoResponse>> Cancelar(int id, [FromBody] MotivoRequest request)
    {
        try
        {
            return Ok(await documentoAppService.CancelarAsync(id, request.Motivo, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("{id:int}/assinar")]
    [EndpointSummary("Assinar digitalmente o documento com o certificado A1 (ICP-Brasil) do médico")]
    [ProducesResponseType(typeof(DocumentoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DocumentoResponse>> Assinar(int id)
    {
        try
        {
            return Ok(await assinaturaAppService.AssinarDocumentoAsync(id, User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/pdf")]
    [EndpointSummary("Baixar o documento em PDF (assinado digitalmente, se já foi assinado)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Pdf(int id)
    {
        var arquivo = await assinaturaAppService.ObterPdfAsync(id);
        if (arquivo is null) return NotFound(new { message = $"Documento com id {id} não encontrado." });
        var disposicao = new ContentDispositionHeaderValue("attachment");
        disposicao.SetHttpFileName(arquivo.NomeArquivo);
        Response.Headers[HeaderNames.ContentDisposition] = disposicao.ToString();
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(arquivo.Conteudo, arquivo.ContentType);
    }
}
