using Consultorio.Aplicacao.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace Consultorio.Server.Servicos;

/// <summary>E-mails da equipe por perfil, a partir dos usuários do Identity (só contas confirmadas e não bloqueadas).</summary>
public class DestinatariosInternos(UserManager<IdentityUser> userManager) : IDestinatariosInternos
{
    public async Task<List<string>> ObterEmailsAsync(params string[] perfis)
    {
        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var perfil in perfis)
        {
            foreach (var usuario in await userManager.GetUsersInRoleAsync(perfil))
            {
                var bloqueado = usuario.LockoutEnd.HasValue && usuario.LockoutEnd.Value > DateTimeOffset.UtcNow;
                if (!string.IsNullOrWhiteSpace(usuario.Email) && usuario.EmailConfirmed && !bloqueado)
                    emails.Add(usuario.Email);
            }
        }
        return emails.ToList();
    }
}
