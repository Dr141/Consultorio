using System.Globalization;
using System.Net;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>Envio pelo primeiro canal disponível e modelo visual comum das mensagens automáticas.</summary>
public static class Notificacoes
{
    /// <summary>
    /// Cultura pt-BR. Em contêineres sem ICU (globalização invariável), "pt-BR" não existe e o construtor
    /// lança exceção; nesse caso usa uma cópia da cultura invariável com moeda, números e datas em português.
    /// </summary>
    public static readonly CultureInfo PtBr = CriarPtBr();

    private static CultureInfo CriarPtBr()
    {
        try
        {
            var cultura = CultureInfo.GetCultureInfo("pt-BR");
            // Sem ICU, alguns ambientes devolvem uma cultura "fantasma" com formatos invariáveis.
            if (cultura.NumberFormat.CurrencySymbol == "R$") return cultura;
        }
        catch (CultureNotFoundException)
        {
        }

        var copia = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        var numeros = copia.NumberFormat;
        numeros.CurrencySymbol = "R$";
        numeros.CurrencyDecimalSeparator = ",";
        numeros.CurrencyGroupSeparator = ".";
        numeros.CurrencyPositivePattern = 2; // "R$ 1.234,56"
        numeros.CurrencyNegativePattern = 9;
        numeros.NumberDecimalSeparator = ",";
        numeros.NumberGroupSeparator = ".";
        numeros.PercentDecimalSeparator = ",";
        numeros.PercentGroupSeparator = ".";
        var datas = copia.DateTimeFormat;
        datas.DayNames = ["domingo", "segunda-feira", "terça-feira", "quarta-feira", "quinta-feira", "sexta-feira", "sábado"];
        datas.AbbreviatedDayNames = ["dom", "seg", "ter", "qua", "qui", "sex", "sáb"];
        datas.MonthNames = ["janeiro", "fevereiro", "março", "abril", "maio", "junho", "julho", "agosto", "setembro", "outubro", "novembro", "dezembro", ""];
        datas.MonthGenitiveNames = datas.MonthNames;
        datas.AbbreviatedMonthNames = ["jan", "fev", "mar", "abr", "mai", "jun", "jul", "ago", "set", "out", "nov", "dez", ""];
        datas.AbbreviatedMonthGenitiveNames = datas.AbbreviatedMonthNames;
        datas.ShortDatePattern = "dd/MM/yyyy";
        datas.LongTimePattern = "HH:mm:ss";
        datas.ShortTimePattern = "HH:mm";
        return CultureInfo.ReadOnly(copia);
    }

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
