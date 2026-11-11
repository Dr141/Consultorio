using System.Net;
using System.Net.Mail;
using Consultorio.Dominio.Interfaces.Servicos;
using Microsoft.Extensions.Logging;

namespace Consultorio.Infraestrutura.Notificacoes;

/// <summary>Envio de mensagens por e-mail via SMTP (qualquer provedor: Gmail, Outlook, SendGrid, Amazon SES...).</summary>
public class EmailNotificador(OpcoesEmail opcoes, ILogger<EmailNotificador> logger) : INotificador
{
    public string Canal => "E-mail";

    public bool PodeEnviar(MensagemNotificacao mensagem) =>
        opcoes.Configurado && !string.IsNullOrWhiteSpace(mensagem.Email);

    public async Task<bool> EnviarAsync(MensagemNotificacao mensagem, CancellationToken cancellationToken = default)
    {
        if (!PodeEnviar(mensagem)) return false;

        try
        {
            using var email = new MailMessage
            {
                From = new MailAddress(opcoes.Remetente!, opcoes.NomeRemetente),
                Subject = mensagem.Assunto,
                Body = mensagem.Html ?? mensagem.Texto,
                IsBodyHtml = mensagem.Html is not null
            };
            email.To.Add(new MailAddress(mensagem.Email!, mensagem.NomeDestinatario));
            if (mensagem.Html is not null)
            {
                email.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(mensagem.Texto, null, "text/plain"));
            }

            using var cliente = new SmtpClient(opcoes.Host, opcoes.Porta)
            {
                EnableSsl = opcoes.UsarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            if (!string.IsNullOrWhiteSpace(opcoes.Usuario))
            {
                cliente.Credentials = new NetworkCredential(opcoes.Usuario, opcoes.Senha);
            }

            await cliente.SendMailAsync(email, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // O e-mail do paciente não vai para o log (dado pessoal); só o motivo da falha.
            logger.LogWarning(ex, "Falha ao enviar e-mail: {Mensagem}", ex.Message);
            return false;
        }
    }
}
