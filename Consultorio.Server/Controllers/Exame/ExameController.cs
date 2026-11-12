using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Excecoes;
using Consultorio.Server.Extensoes;
using Consultorio.Server.Filtros;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

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

    [HttpPatch("{id:int}/realizado")]
    [EndpointSummary("Registrar que o exame foi realizado (coleta/execução)")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> RegistrarRealizacao(int id)
    {
        try
        {
            return Ok(await exameAppService.RegistrarRealizacaoAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:int}/entregue")]
    [EndpointSummary("Registrar a entrega do resultado ao paciente")]
    [ProducesResponseType(typeof(ExameResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameResponse>> RegistrarEntrega(int id)
    {
        try
        {
            return Ok(await exameAppService.RegistrarEntregaAsync(id));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/anexos")]
    [EndpointSummary("Listar arquivos anexados ao exame")]
    [ProducesResponseType(typeof(List<ExameAnexoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ExameAnexoResponse>>> ObterAnexos(int id)
    {
        return Ok(await exameAppService.ObterAnexosAsync(id));
    }

    [HttpPost("{id:int}/anexos")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    [EndpointSummary("Anexar resultado do exame (PDF, JPG ou PNG, até 10 MB)")]
    [ProducesResponseType(typeof(ExameAnexoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExameAnexoResponse>> AdicionarAnexo(int id, IFormFile? arquivo, CancellationToken cancellationToken)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(new { message = "Selecione um arquivo." });
        if (arquivo.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "O arquivo ultrapassa o limite de 10 MB." });

        try
        {
            using var memoria = new MemoryStream();
            await arquivo.CopyToAsync(memoria, cancellationToken);
            return Ok(await exameAppService.AdicionarAnexoAsync(id, arquivo.FileName, memoria.ToArray(), User.ObterEmail()));
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}/anexos/{idAnexo:int}")]
    [EndpointSummary("Abrir/baixar um arquivo anexado ao exame")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObterArquivoAnexo(int id, int idAnexo)
    {
        var arquivo = await exameAppService.ObterArquivoAnexoAsync(id, idAnexo);
        if (arquivo is null) return NotFound(new { message = "Arquivo não encontrado." });

        // "inline" abre PDF/imagem no navegador; nosniff impede que o conteúdo seja interpretado como outro tipo.
        var disposicao = new ContentDispositionHeaderValue("inline");
        disposicao.SetHttpFileName(arquivo.NomeArquivo);
        Response.Headers[HeaderNames.ContentDisposition] = disposicao.ToString();
        Response.Headers[HeaderNames.XContentTypeOptions] = "nosniff";
        return File(arquivo.Conteudo, arquivo.ContentType);
    }
}
