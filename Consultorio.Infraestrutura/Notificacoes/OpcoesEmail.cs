namespace Consultorio.Infraestrutura.Notificacoes;

/// <summary>Configuração do envio de e-mails (seção "Notificacoes:Email" do appsettings).</summary>
public class OpcoesEmail
{
    public string? Host { get; set; }
    public int Porta { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string? Usuario { get; set; }
    public string? Senha { get; set; }
    public string? Remetente { get; set; }
    public string NomeRemetente { get; set; } = "Consultório";

    public bool Configurado => !string.IsNullOrWhiteSpace(Host) && !string.IsNullOrWhiteSpace(Remetente);
}
