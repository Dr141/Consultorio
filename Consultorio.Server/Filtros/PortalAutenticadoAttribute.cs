using System.Security.Claims;
using Consultorio.Aplicacao.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Consultorio.Server.Filtros;

/// <summary>
/// Exige uma sessão válida do portal do paciente (cabeçalho X-Portal-Token).
/// O id do paciente da sessão fica em HttpContext.Items e a identidade vira "portal:paciente:{id}",
/// sem nenhum perfil da equipe — assim a auditoria registra o acesso e nenhuma política interna é satisfeita.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class PortalAutenticadoAttribute : TypeFilterAttribute
{
    public PortalAutenticadoAttribute() : base(typeof(PortalAutenticacaoFilter)) { }
}

public class PortalAutenticacaoFilter(IPortalPacienteAppService portal) : IAsyncAuthorizationFilter
{
    public const string Cabecalho = "X-Portal-Token";
    public const string ChaveIdPaciente = "PortalIdPaciente";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var token = context.HttpContext.Request.Headers[Cabecalho].ToString();
        var idPaciente = await portal.ValidarSessaoAsync(token);
        if (idPaciente is null)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Sua sessão no portal expirou. Entre novamente." });
            return;
        }

        context.HttpContext.Items[ChaveIdPaciente] = idPaciente.Value;
        var identidade = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, $"portal:paciente:{idPaciente.Value}"),
            new Claim(ClaimTypes.Email, $"portal:paciente:{idPaciente.Value}")
        ], "Portal");
        context.HttpContext.User = new ClaimsPrincipal(identidade);
    }
}

public static class PortalHttpContextExtensoes
{
    public static int ObterIdPacientePortal(this HttpContext contexto) =>
        contexto.Items.TryGetValue(PortalAutenticacaoFilter.ChaveIdPaciente, out var valor) && valor is int id
            ? id
            : throw new UnauthorizedAccessException("Sessão do portal não identificada.");
}
