using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Consultorio.Server.Extensoes;

/// <summary>Limite de tentativas na entrada do portal, por IP (freia adivinhação de CPF e de código).</summary>
public static class PoliticaLimitePortal
{
    public const string Nome = "portal-acesso";

    public static IServiceCollection AdicionarLimitePortal(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (contexto, token) =>
            {
                contexto.HttpContext.Response.ContentType = "application/json; charset=utf-8";
                await contexto.HttpContext.Response.WriteAsync(
                    "{\"message\":\"Muitas tentativas em pouco tempo. Aguarde alguns minutos e tente novamente.\"}", token);
            };
            options.AddPolicy(Nome, contexto => RateLimitPartition.GetFixedWindowLimiter(
                contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(5),
                    QueueLimit = 0
                }));
        });
        return services;
    }
}
