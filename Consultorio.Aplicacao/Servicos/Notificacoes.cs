using System.Globalization;
using System.Net;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>Envio pelo primeiro canal disponível e modelo visual comum das mensagens automáticas.</summary>
public static class Notificacoes
{
    public static readonly CultureInfo PtBr = new("pt-BR");

    /// <summary>true = enviado; false = falhou; null = sem canal/contato para o destinatário.</summary>
    public static async Task<bool?> EnviarAsync(IEnumerable<INotificador> notificadores, MensagemNotificacao mensagem, CancellationToken cancellationToken)
    {
        var canal = notificadores.FirstOrDefault(n => n.PodeEnviar(mensagem));
        if (canal is null) return null;
        try
        {
            return await canal.EnviarAsync(mensagem, cancellationToken);
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string H(string? valor) => WebUtility.HtmlEncode(valor ?? string.Empty);

    /// <summary>Corpo HTML simples: título, parágrafos (já codificados) e rodapé da clínica.</summary>
    public static string Html(OpcoesClinica opcoes, string titulo, IEnumerable<string> paragrafosHtml)
    {
        var corpo = string.Concat(paragrafosHtml.Select(p => $"<p style=\"margin:0 0 12px\">{p}</p>"));
        var contato = string.Join(" · ", new[] { opcoes.Endereco, opcoes.Telefone }.Where(v => !string.IsNullOrWhiteSpace(v)).Select(H));
        return $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:auto;color:#0f172a">
              <h2 style="color:#1976d2;margin:0 0 4px">{H(opcoes.Nome)}</h2>
              <h3 style="margin:0 0 16px;font-weight:600">{H(titulo)}</h3>
              {corpo}
              {(contato.Length > 0 ? $"<p style=\"color:#475569;font-size:13px\">{contato}</p>" : "")}
              <p style="color:#64748b;font-size:12px">Mensagem automática. Por favor, não responda este e-mail.</p>
            </div>
            """;
    }

    public static string PrimeiroNome(string? nome) =>
        (nome ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
}
