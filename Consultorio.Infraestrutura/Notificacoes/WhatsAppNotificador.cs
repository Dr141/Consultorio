using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Consultorio.Dominio.Interfaces.Servicos;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infraestrutura.Notificacoes;

/// <summary>
/// Envio de mensagens por WhatsApp via WhatsApp Cloud API (Meta). Usa um modelo aprovado
/// ("Olá {{1}}! {{2}}") porque a clínica inicia a conversa; o texto da notificação vira o parâmetro {{2}}.
/// </summary>
public partial class WhatsAppNotificador(HttpClient http, OpcoesWhatsApp opcoes, ILogger<WhatsAppNotificador> logger) : INotificador
{
    public string Canal => "WhatsApp";

    public bool PodeEnviar(MensagemNotificacao mensagem) =>
        opcoes.Configurado && NumeroWhatsApp(mensagem.Telefone, opcoes.Pais) is not null;

    public async Task<bool> EnviarAsync(MensagemNotificacao mensagem, CancellationToken cancellationToken = default)
    {
        var numero = NumeroWhatsApp(mensagem.Telefone, opcoes.Pais);
        if (!opcoes.Configurado || numero is null) return false;

        var corpo = new
        {
            messaging_product = "whatsapp",
            to = numero,
            type = "template",
            template = new
            {
                name = opcoes.Template,
                language = new { code = opcoes.Idioma },
                components = new object[]
                {
                    new
                    {
                        type = "body",
                        parameters = new object[]
                        {
                            new { type = "text", text = PrimeiroNome(mensagem.NomeDestinatario) },
                            new { type = "text", text = TextoParametro(mensagem.Texto, opcoes.TamanhoMaximoTexto) }
                        }
                    }
                }
            }
        };

        try
        {
            var url = $"{opcoes.UrlBase.TrimEnd('/')}/{opcoes.VersaoApi}/{opcoes.PhoneNumberId}/messages";
            using var requisicao = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(corpo) };
            requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opcoes.Token);

            using var resposta = await http.SendAsync(requisicao, cancellationToken);
            if (resposta.IsSuccessStatusCode) return true;

            // O corpo do erro da Meta traz o código/motivo (sem dados do paciente).
            var erro = await resposta.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("WhatsApp recusou a mensagem ({Status}): {Erro}", (int)resposta.StatusCode,
                erro.Length > 500 ? erro[..500] : erro);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Falha ao enviar WhatsApp: {Mensagem}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Número no formato E.164 sem "+" (ex.: 5511987654321), apenas para celulares brasileiros
    /// (DDD + 9 dígitos começando por 9). Fixos não têm WhatsApp garantido, então ficam para o e-mail.
    /// </summary>
    public static string? NumeroWhatsApp(string? telefone, string pais = "55")
    {
        if (string.IsNullOrWhiteSpace(telefone)) return null;
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        if (digitos.Length == 13 && digitos.StartsWith(pais, StringComparison.Ordinal)) digitos = digitos[pais.Length..];
        if (digitos.Length == 12 && digitos.StartsWith('0')) digitos = digitos[1..]; // 0 + DDD + número
        if (digitos.Length != 11 || digitos[2] != '9' || digitos[0] == '0') return null;
        return pais + digitos;
    }

    /// <summary>
    /// Parâmetros de modelo não aceitam quebras de linha, tabulações nem mais de 4 espaços seguidos.
    /// A saudação inicial ("Olá, Maria!") é removida porque o próprio modelo já cumprimenta com {{1}}.
    /// </summary>
    public static string TextoParametro(string? texto, int tamanhoMaximo = 900)
    {
        var semSaudacao = Saudacao().Replace(texto ?? string.Empty, string.Empty, 1);
        var limpo = Espacos().Replace(semSaudacao, " ").Trim();
        if (limpo.Length == 0) return "-";
        return limpo.Length <= tamanhoMaximo ? limpo : limpo[..(tamanhoMaximo - 1)].TrimEnd() + "…";
    }

    private static string PrimeiroNome(string? nome)
    {
        var primeiro = (nome ?? string.Empty).Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrEmpty(primeiro) ? "paciente" : primeiro;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Espacos();

    [GeneratedRegex(@"^\s*(Olá|Oi)\b[^!\n]{0,60}!\s*", RegexOptions.IgnoreCase)]
    private static partial Regex Saudacao();
}
