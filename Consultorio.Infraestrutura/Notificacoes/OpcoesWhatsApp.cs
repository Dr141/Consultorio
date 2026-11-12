namespace Consultorio.Infraestrutura.Notificacoes;

/// <summary>
/// Configuração do envio por WhatsApp (seção "Notificacoes:WhatsApp"), usando a API oficial
/// WhatsApp Cloud API da Meta. Mensagens iniciadas pela empresa exigem um modelo (template)
/// aprovado; o modelo padrão esperado tem dois parâmetros no corpo: {{1}} = nome, {{2}} = texto.
/// </summary>
public class OpcoesWhatsApp
{
    public bool Habilitado { get; set; }

    /// <summary>Token de acesso permanente (usuário do sistema no Meta Business).</summary>
    public string? Token { get; set; }

    /// <summary>Identificador do número de telefone remetente (Phone number ID, não o número em si).</summary>
    public string? PhoneNumberId { get; set; }

    public string VersaoApi { get; set; } = "v21.0";
    public string UrlBase { get; set; } = "https://graph.facebook.com";
    public string Template { get; set; } = "aviso_clinica";
    public string Idioma { get; set; } = "pt_BR";

    /// <summary>Código do país acrescentado aos telefones (somente dígitos).</summary>
    public string Pais { get; set; } = "55";

    /// <summary>Limite do texto enviado no parâmetro {{2}} (a Meta aceita até 1024 caracteres por parâmetro).</summary>
    public int TamanhoMaximoTexto { get; set; } = 900;

    public bool Configurado =>
        Habilitado && !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(PhoneNumberId);
}
