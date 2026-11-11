using System.Security.Claims;
using Consultorio.Aplicacao.DTOs.Request;

namespace Consultorio.Server.Extensoes;

public static class UsuarioExtensoes
{
    /// <summary>E-mail do usuário autenticado (claim "email" do JWT, com ou sem mapeamento de tipos).</summary>
    public static string ObterEmail(this ClaimsPrincipal? usuario) =>
        usuario?.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
        ?? usuario?.FindFirst("email")?.Value
        ?? usuario?.Identity?.Name
        ?? string.Empty;

    public static UsuarioAtual ObterUsuarioAtual(this ClaimsPrincipal? usuario) =>
        new(usuario.ObterEmail(), usuario?.IsInRole(nameof(global::Consultorio.Identity.Modelo.Enumerados.Roles.Administrador)) ?? false);
}
