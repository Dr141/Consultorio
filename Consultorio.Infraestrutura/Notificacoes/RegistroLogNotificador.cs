using Consultorio.Dominio.Interfaces.Servicos;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infraestrutura.Notificacoes;

/// <summary>
/// Canal de desenvolvimento: quando habilitado ("Notificacoes:SimularEnvio": true), em vez de enviar
/// apenas registra no log que a mensagem seria enviada. Útil para testar as rotinas sem SMTP.
/// </summary>
public class RegistroLogNotificador(ILogger<RegistroLogNotificador> logger) : INotificador
{
    public string Canal => "Log (simulação)";

    public bool PodeEnviar(MensagemNotificacao mensagem) =>
        !string.IsNullOrWhiteSpace(mensagem.Email) || !string.IsNullOrWhiteSpace(mensagem.Telefone);

    public Task<bool> EnviarAsync(MensagemNotificacao mensagem, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("[Simulação] Mensagem \"{Assunto}\" seria enviada para {Nome}.", mensagem.Assunto, mensagem.NomeDestinatario);
        return Task.FromResult(true);
    }
}
