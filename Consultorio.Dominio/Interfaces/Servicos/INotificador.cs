namespace Consultorio.Dominio.Interfaces.Servicos;

/// <summary>Mensagem a ser enviada ao paciente por um canal (e-mail, WhatsApp, SMS...).</summary>
public record MensagemNotificacao(
    string NomeDestinatario,
    string? Email,
    string? Telefone,
    string Assunto,
    string Texto,
    string? Html = null
);

/// <summary>
/// Canal de envio de mensagens. Para integrar WhatsApp ou SMS, implemente esta interface
/// (ex.: chamando a API do provedor) e registre a classe no contêiner de DI; as rotinas usam
/// o primeiro canal disponível que consiga atender o destinatário.
/// </summary>
public interface INotificador
{
    string Canal { get; }

    /// <summary>O canal está configurado e consegue enviar para este destinatário?</summary>
    bool PodeEnviar(MensagemNotificacao mensagem);

    Task<bool> EnviarAsync(MensagemNotificacao mensagem, CancellationToken cancellationToken = default);
}
