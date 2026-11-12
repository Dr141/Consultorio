using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Identity.Modelo.Enumerados;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Server.Servicos;

/// <summary>
/// Revisão periódica de acessos (LGPD / boas práticas de prontuário eletrônico): quem tem acesso,
/// com quais perfis e quando usou o sistema pela última vez. Uma vez por mês os administradores
/// recebem um lembrete com as contas que merecem revisão.
/// </summary>
public class RevisaoAcessosService(
    UserManager<IdentityUser> userManager,
    IAuditoriaRepositorio auditoriaRepositorio,
    IExecucaoRotinaRepositorio execucaoRepositorio,
    IEnumerable<INotificador> notificadores,
    OpcoesClinica opcoes,
    TimeProvider relogio)
{
    public const string Rotina = "revisao-acessos";

    public async Task<List<AcessoUsuarioResponse>> ObterAsync()
    {
        var agora = relogio.GetUtcNow().UtcDateTime;
        var limiteDias = Math.Max(1, opcoes.DiasInatividadeRevisaoAcessos);
        var atividades = await auditoriaRepositorio.UltimaAtividadePorUsuario();
        var usuarios = await userManager.Users.AsNoTracking().OrderBy(u => u.Email).ToListAsync();

        var resultado = new List<AcessoUsuarioResponse>();
        foreach (var usuario in usuarios)
        {
            var email = usuario.Email ?? usuario.UserName ?? usuario.Id;
            var perfis = (await userManager.GetRolesAsync(usuario)).OrderBy(p => p).ToList();
            var bloqueado = usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > DateTimeOffset.UtcNow;
            DateTime? ultima = atividades.TryGetValue(email.ToLowerInvariant(), out var data) ? data : null;
            int? dias = ultima.HasValue ? (int)Math.Floor((agora - ultima.Value).TotalDays) : null;

            string? motivo = null;
            if (bloqueado) motivo = null;
            else if (perfis.Count == 0) motivo = "Conta sem perfil: não acessa nenhuma tela.";
            else if (ultima is null) motivo = "Nenhuma atividade registrada.";
            else if (dias > limiteDias) motivo = $"Sem atividade há {dias} dias.";
            else if (perfis.Contains(nameof(Roles.Administrador))) motivo = "Perfil Administrador: confirme se ainda é necessário.";

            resultado.Add(new AcessoUsuarioResponse(email, perfis, usuario.EmailConfirmed, bloqueado, ultima, dias, motivo is not null, motivo));
        }

        return resultado
            .OrderByDescending(r => r.RevisarAcesso)
            .ThenBy(r => r.Email, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>No início do mês, avisa os administradores das contas a revisar. Retorna true se enviou.</summary>
    public async Task<bool> EnviarLembreteMensalAsync(CancellationToken cancellationToken = default)
    {
        var agoraLocal = TimeZoneInfo.ConvertTimeFromUtc(relogio.GetUtcNow().UtcDateTime, opcoes.ObterFuso());
        if (agoraLocal.Hour < Math.Clamp(opcoes.HoraResumoDiario, 0, 23)) return false;
        var referencia = agoraLocal.ToString("yyyy-MM");

        var execucao = await execucaoRepositorio.ObterPorNome(Rotina);
        if (execucao?.JaExecutou(referencia) == true) return false;
        if (execucao is null) await execucaoRepositorio.Adicionar(new ExecucaoRotina(Rotina, referencia));
        else
        {
            execucao.Registrar(referencia);
            await execucaoRepositorio.Atualizar(execucao);
        }

        var revisar = (await ObterAsync()).Where(a => a.RevisarAcesso).ToList();
        if (revisar.Count == 0) return false;

        var admins = await userManager.GetUsersInRoleAsync(nameof(Roles.Administrador));
        var linhas = revisar.Select(r => $"{r.Email} ({(r.Perfis.Count == 0 ? "sem perfil" : string.Join(", ", r.Perfis))}): {r.MotivoRevisao}").ToList();
        var texto = $"Revisão mensal de acessos — {opcoes.Nome}\n\n" + string.Join("\n", linhas.Select(l => $"- {l}")) +
                    "\n\nRemova perfis e bloqueie contas que não são mais necessárias em Usuários, no sistema.";
        var html = Notificacoes.Html(opcoes, "Revisão mensal de acessos",
            linhas.Select(Notificacoes.H).Append("Remova perfis e bloqueie contas que não são mais necessárias em <strong>Usuários</strong>, no sistema."));

        foreach (var admin in admins.Where(a => !string.IsNullOrWhiteSpace(a.Email) && a.EmailConfirmed))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await Notificacoes.EnviarAsync(notificadores,
                new MensagemNotificacao(admin.Email!, admin.Email, null, $"Revisão de acessos ({revisar.Count} conta(s))", texto, html),
                cancellationToken);
        }
        return true;
    }
}
