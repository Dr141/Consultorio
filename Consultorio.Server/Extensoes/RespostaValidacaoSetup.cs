using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Extensoes;

/// <summary>
/// Padroniza a resposta de erros de validação do modelo para { message, errors },
/// trocando mensagens técnicas de conversão de JSON por textos em português.
/// </summary>
public static class RespostaValidacaoSetup
{
    public static IMvcBuilder ConfigurarRespostaValidacao(this IMvcBuilder builder)
    {
        return builder.ConfigureApiBehaviorOptions(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var erros = context.ModelState
                    .Where(item => item.Value is not null && item.Value.Errors.Count > 0)
                    .ToDictionary(
                        item => NomeCampo(item.Key),
                        item => item.Value!.Errors.Select(e => TraduzirMensagem(item.Key, e.ErrorMessage, e.Exception)).Distinct().ToArray());

                var primeiraMensagem = erros.Values.SelectMany(m => m).FirstOrDefault()
                                       ?? "Os dados enviados são inválidos. Revise os campos e tente novamente.";

                return new BadRequestObjectResult(new { message = primeiraMensagem, errors = erros });
            };
        });
    }

    private static string NomeCampo(string chave)
    {
        var campo = chave.TrimStart('$', '.');
        return string.IsNullOrWhiteSpace(campo) ? "requisicao" : campo;
    }

    private static string TraduzirMensagem(string chave, string? mensagem, Exception? excecao)
    {
        var campo = NomeCampo(chave);
        var ehErroTecnico = excecao is not null
                            || string.IsNullOrWhiteSpace(mensagem)
                            || mensagem.Contains("JSON", StringComparison.OrdinalIgnoreCase)
                            || mensagem.Contains("field is required", StringComparison.OrdinalIgnoreCase);

        if (!ehErroTecnico) return mensagem!;

        return campo == "requisicao"
            ? "Os dados enviados estão em um formato inválido."
            : $"Valor inválido ou ausente para o campo '{campo}'.";
    }
}
