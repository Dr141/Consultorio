using Consultorio.Aplicacao.Interfaces;
using Consultorio.Server.Extensoes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Consultorio.Server.Filtros;

/// <summary>
/// Registra na trilha de auditoria cada acesso às ações do controller (leituras e alterações).
/// Uso: [Auditar("Consulta")] no controller ou na action.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class AuditarAttribute : TypeFilterAttribute
{
    public AuditarAttribute(string recurso) : base(typeof(AuditoriaFilter))
    {
        Arguments = [recurso];
    }
}

public class AuditoriaFilter(string recurso, IAuditoriaAppService auditoria, ILogger<AuditoriaFilter> logger) : IAsyncActionFilter
{
    private static readonly string[] ChavesId = ["id", "idPaciente", "idMedico"];

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executado = await next();

        try
        {
            var http = context.HttpContext;
            var status = executado.Exception is not null && !executado.ExceptionHandled
                ? StatusCodes.Status500InternalServerError
                : executado.Result switch
                {
                    IStatusCodeActionResult r when r.StatusCode.HasValue => r.StatusCode.Value,
                    _ => http.Response.StatusCode
                };

            string? idRecurso = null;
            foreach (var chave in ChavesId)
            {
                if (context.RouteData.Values.TryGetValue(chave, out var valor) && valor is not null)
                {
                    idRecurso = chave == "id" ? valor.ToString() : $"{chave}:{valor}";
                    break;
                }
            }

            await auditoria.RegistrarAsync(
                http.User.ObterEmail(),
                DescreverAcao(http.Request.Method, context.ActionDescriptor.RouteValues.TryGetValue("action", out var a) ? a : null),
                recurso,
                idRecurso,
                http.Request.Method,
                $"{http.Request.Path}{http.Request.QueryString}",
                status,
                http.Connection.RemoteIpAddress?.ToString());
        }
        catch (Exception ex)
        {
            // A auditoria nunca deve derrubar a requisição do usuário, mas a falha precisa ficar visível no log.
            logger.LogError(ex, "Falha ao registrar auditoria de {Recurso}", recurso);
        }
    }

    private static string DescreverAcao(string metodo, string? action) => metodo.ToUpperInvariant() switch
    {
        "GET" => "Leitura",
        "POST" => action is not null && action.Contains("Adendo", StringComparison.OrdinalIgnoreCase) ? "Adendo" : "Criacao",
        "PUT" => "Alteracao",
        "PATCH" => action ?? "Alteracao",
        "DELETE" => "Exclusao",
        _ => metodo
    };
}
