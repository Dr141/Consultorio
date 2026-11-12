using System.Net;
using Consultorio.Aplicacao.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Consultorio.Server.Controllers.Publico;

/// <summary>
/// Página pública para conferir a autenticidade de um documento pelo código impresso nele
/// (empresa que recebe um atestado, farmácia que recebe uma receita).
/// Mostra só o essencial: tipo, data, médico/CRM, iniciais do paciente e situação — nunca o conteúdo clínico.
/// </summary>
[Route("api/publico/documento")]
[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class VerificacaoDocumentoController(IDocumentoAppService documentoAppService) : ControllerBase
{
    [HttpGet("{codigo}")]
    public async Task<ContentResult> Verificar(string codigo)
    {
        var codigoLimpo = new string((codigo ?? string.Empty).Where(char.IsLetterOrDigit).Take(20).ToArray());
        var r = await documentoAppService.VerificarAsync(codigoLimpo);
        static string H(string? v) => WebUtility.HtmlEncode(v ?? string.Empty);

        string corpo;
        if (!r.Encontrado)
        {
            corpo = $"""
                <div class="selo invalido">Documento não encontrado</div>
                <p>Nenhum documento corresponde ao código <strong>{H(codigoLimpo)}</strong>. Confira se ele foi digitado corretamente.</p>
                """;
        }
        else
        {
            var situacao = r.Cancelado
                ? "<div class=\"selo invalido\">Documento CANCELADO pelo emissor</div>"
                : "<div class=\"selo valido\">Documento autêntico</div>";
            var afastamento = r.DiasAfastamento.HasValue ? $"<dt>Afastamento</dt><dd>{r.DiasAfastamento} dia(s)</dd>" : "";
            corpo = $"""
                {situacao}
                <dl>
                  <dt>Documento</dt><dd>{H(r.TipoNome)}</dd>
                  <dt>Emitido em</dt><dd>{r.DtEmissao?.ToLocalTime().ToString("dd/MM/yyyy HH:mm")}</dd>
                  <dt>Médico</dt><dd>{H(r.NomeMedico)} — CRM {H(r.CrmMedico)}</dd>
                  <dt>Paciente</dt><dd>{H(r.IniciaisPaciente)}</dd>
                  {afastamento}
                  <dt>Código</dt><dd>{H(codigoLimpo)}</dd>
                </dl>
                """;
        }

        var html = $$"""
            <!DOCTYPE html>
            <html lang="pt-BR">
            <head>
              <meta charset="utf-8" />
              <meta name="viewport" content="width=device-width, initial-scale=1" />
              <meta name="robots" content="noindex" />
              <title>Verificação de documento · {{H(r.NomeClinica)}}</title>
              <style>
                body { font-family: system-ui, -apple-system, Segoe UI, Roboto, Arial, sans-serif; background:#f8fafc; color:#0f172a; margin:0; padding:24px; }
                main { max-width:520px; margin:40px auto; background:#fff; border-radius:16px; padding:28px; box-shadow:0 8px 24px rgba(15,23,42,.08); }
                h1 { font-size:20px; margin:0 0 16px; color:#1976d2; }
                .selo { font-weight:700; padding:12px 14px; border-radius:10px; margin-bottom:16px; }
                .valido { background:#dcfce7; color:#166534; }
                .invalido { background:#fee2e2; color:#991b1b; }
                dl { display:grid; grid-template-columns:max-content 1fr; gap:8px 16px; margin:0; }
                dt { color:#64748b; }
                dd { margin:0; font-weight:600; }
              </style>
            </head>
            <body>
              <main>
                <h1>{{H(r.NomeClinica)}} · Verificação de documento</h1>
                {{corpo}}
              </main>
            </body>
            </html>
            """;

        return new ContentResult { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = 200 };
    }
}
