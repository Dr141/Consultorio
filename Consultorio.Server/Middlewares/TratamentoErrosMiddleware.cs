using Consultorio.Dominio.Excecoes;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Server.Middlewares;

/// <summary>
/// Converte exceções não tratadas em respostas JSON padronizadas no formato { message, traceId },
/// para que o frontend sempre consiga exibir uma mensagem clara ao usuário.
/// </summary>
public class TratamentoErrosMiddleware(RequestDelegate next, ILogger<TratamentoErrosMiddleware> logger)
{
    public const string MensagemErroInesperado =
        "Ocorreu um erro inesperado ao processar sua solicitação. Tente novamente em instantes.";

    public const string MensagemConflito =
        "Não foi possível salvar: já existe um registro com esses dados ou ele está vinculado a outras informações.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Cliente cancelou a requisição (ex.: fechou a tela). Nada a responder.
        }
        catch (Exception ex)
        {
            var (status, mensagem) = Mapear(ex);

            if (status >= StatusCodes.Status500InternalServerError)
                logger.LogError(ex, "Erro não tratado em {Metodo} {Caminho}", context.Request.Method, context.Request.Path);
            else
                logger.LogWarning("Requisição recusada ({Status}) em {Metodo} {Caminho}: {Mensagem}", status, context.Request.Method, context.Request.Path, ex.Message);

            if (context.Response.HasStarted) throw;

            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { message = mensagem, traceId = context.TraceIdentifier });
        }
    }

    public static (int Status, string Mensagem) Mapear(Exception ex) => ex switch
    {
        DomainException => (StatusCodes.Status400BadRequest, ex.Message),
        ArgumentNullException => (StatusCodes.Status500InternalServerError, MensagemErroInesperado),
        ArgumentException => (StatusCodes.Status400BadRequest, ex.Message),
        UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, ex.Message),
        KeyNotFoundException => (StatusCodes.Status404NotFound, ex.Message),
        DbUpdateException => (StatusCodes.Status409Conflict, MensagemConflito),
        _ => (StatusCodes.Status500InternalServerError, MensagemErroInesperado)
    };
}
