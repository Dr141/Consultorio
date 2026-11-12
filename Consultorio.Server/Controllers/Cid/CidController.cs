using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Server.Extensoes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Cid;

[Route("api/[controller]")]
[Authorize(Policy = PoliticasAcesso.Clinico)]
[ApiController]
public class CidController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Buscar códigos CID-10 frequentes por código ou descrição")]
    [ProducesResponseType(typeof(List<CidResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<CidResponse>> Buscar([FromQuery] string? busca, [FromQuery] int limite = 20)
    {
        return Ok(CatalogoCid.Buscar(busca, limite));
    }
}
