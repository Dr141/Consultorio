using System.Net;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Publico;

/// <summary>
/// Página pública (sem login) aberta pelo paciente a partir do lembrete.
/// GET apenas exibe o horário e os botões; a ação acontece no POST. Assim, filtros de e-mail
/// e antivírus que "abrem" os links automaticamente não confirmam nem desmarcam nada.
/// </summary>
[Route("api/publico/confirmacao")]
[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class ConfirmacaoController(IAgendaAppService agendaAppService, OpcoesClinica opcoes) : ControllerBase
{
    [HttpGet("{token:guid}")]
    public async Task<ContentResult> Exibir(Guid token, [FromQuery] string? resposta)
    {
        var agenda = (await agendaAppService.ObterPorTokenAsync(token));
        if (agenda is null)
        {
            return Pagina("Link inválido", "Este link não é mais válido. O horário pode ter sido desmarcado ou alterado. Entre em contato com a clínica.", null, null);
        }

        var querDesmarcar = string.Equals(resposta, "nao", StringComparison.OrdinalIgnoreCase);
        var formulario = $"""
            <form method="post" action="{WebUtility.HtmlEncode(Request.Path)}">
              <button name="resposta" value="sim" class="btn confirmar" {(querDesmarcar ? "" : "autofocus")}>Confirmar presença</button>
              <button name="resposta" value="nao" class="btn desmarcar" {(querDesmarcar ? "autofocus" : "")}>Não poderei ir — desmarcar</button>
            </form>
            """;
        return Pagina("Seu atendimento", "Confirme se você poderá comparecer:", agenda, formulario);
    }

    [HttpPost("{token:guid}")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<ContentResult> Responder(Guid token, [FromForm] string? resposta)
    {
        var confirmar = !string.Equals(resposta, "nao", StringComparison.OrdinalIgnoreCase);
        var resultado = await agendaAppService.ResponderConfirmacaoAsync(token, confirmar);
        var titulo = resultado.Resultado switch
        {
            ResultadoConfirmacao.Confirmado or ResultadoConfirmacao.JaConfirmado => "Presença confirmada",
            ResultadoConfirmacao.Desmarcado => "Horário desmarcado",
            _ => "Não foi possível concluir"
        };
        return Pagina(titulo, resultado.Mensagem, resultado.Agenda, null);
    }

    private ContentResult Pagina(string titulo, string mensagem, AgendaResponse? agenda, string? conteudoExtra)
    {
        static string H(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);
        var cultura = Consultorio.Aplicacao.Servicos.Notificacoes.PtBr;
        var detalhes = agenda is null ? "" : $"""
            <div class="card">
              <div class="quando">{H(agenda.Data.Date.Add(agenda.Hora).ToString("dddd, dd 'de' MMMM 'às' HH:mm", cultura))}</div>
              {(agenda.NomeMedico is null ? "" : $"<div>com {H(agenda.NomeMedico)}</div>")}
              {(agenda.NomePaciente is null ? "" : $"<div class=\"paciente\">Paciente: {H(agenda.NomePaciente)}</div>")}
            </div>
            """;

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <meta name="robots" content="noindex" />
              <title>{{H(titulo)}} · {{H(opcoes.Nome)}}</title>
              <style>
                body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; background:#f8fafc; color:#0f172a; margin:0; padding:24px; }
                main { max-width:480px; margin:40px auto; background:#fff; border-radius:16px; padding:28px; box-shadow:0 8px 24px rgba(15,23,42,.08); }
                h1 { font-size:22px; margin:0 0 4px; color:#1976d2; }
                h2 { font-size:18px; margin:16px 0 8px; }
                .card { background:#f1f5f9; border-radius:12px; padding:14px 16px; margin:16px 0; }
                .quando { font-size:18px; font-weight:700; text-transform:capitalize; }
                .paciente { color:#64748b; font-size:14px; margin-top:4px; }
                form { display:flex; flex-direction:column; gap:10px; margin-top:16px; }
                .btn { font-size:16px; padding:14px; border-radius:10px; border:0; cursor:pointer; font-weight:600; }
                .confirmar { background:#16a34a; color:#fff; }
                .desmarcar { background:#fff; color:#dc2626; border:1px solid #fecaca; }
                .rodape { color:#64748b; font-size:13px; margin-top:20px; }
              </style>
            </head>
            <body>
              <main>
                <h1>{{H(opcoes.Nome)}}</h1>
                <h2>{{H(titulo)}}</h2>
                <p>{{H(mensagem)}}</p>
                {{detalhes}}
                {{conteudoExtra}}
                <p class="rodape">{{(opcoes.Telefone is null ? "" : "Dúvidas: " + H(opcoes.Telefone))}}</p>
              </main>
            </body>
            </html>
            """;

        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
    }
}
