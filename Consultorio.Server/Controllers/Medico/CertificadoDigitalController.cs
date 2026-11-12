using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Medico;

/// <summary>
/// Certificado digital A1 (.pfx/.p12) do médico, usado para assinar receitas e atestados em PDF.
/// O arquivo e a senha ficam criptografados no servidor e nunca são devolvidos pela API.
/// </summary>
[Route("api/medico/{idMedico:int}/certificado")]
[Authorize(Policy = PoliticasAcesso.Clinico)]
[Auditar("CertificadoDigital")]
[ApiController]
public class CertificadoDigitalController(IAssinaturaDigitalAppService assinaturaAppService) : ControllerBase
{
    private const int LimiteBytes = 64 * 1024;

    [HttpGet]
    [EndpointSummary("Dados do certificado digital ativo do médico (204 se não houver)")]
    [ProducesResponseType(typeof(CertificadoDigitalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<CertificadoDigitalResponse>> Obter(int idMedico)
    {
        var certificado = await assinaturaAppService.ObterCertificadoAsync(idMedico);
        return certificado is null ? NoContent() : Ok(certificado);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LimiteBytes * 2)]
    [RequestFormLimits(MultipartBodyLengthLimit = LimiteBytes * 2)]
    [EndpointSummary("Cadastrar ou substituir o certificado digital A1 do médico")]
    [ProducesResponseType(typeof(CertificadoDigitalResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CertificadoDigitalResponse>> Enviar(int idMedico, IFormFile? arquivo, [FromForm] string? senha,
        CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { message = "Selecione o arquivo do certificado (.pfx ou .p12)." });
        if (arquivo.Length > LimiteBytes)
            return BadRequest(new { message = "Arquivo grande demais para um certificado A1 (.pfx ou .p12)." });

        try
        {
            using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria, cancellationToken);
            return Ok(await assinaturaAppService.EnviarCertificadoAsync(idMedico, memoria.ToArray(), senha ?? string.Empty,
                User.ObterUsuarioAtual()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete]
    [EndpointSummary("Remover o certificado digital do médico (o arquivo e a senha são apagados)")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Remover(int idMedico)
    {
        try
        {
            await assinaturaAppService.RemoverCertificadoAsync(idMedico, User.ObterUsuarioAtual());
            return NoContent();
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
