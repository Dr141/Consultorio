using System.Security.Claims;

namespace Consultorio.Identity.Modelo.DTOs.Share;

public record UsuarioDto(string Email, bool EmailConfirmado, IEnumerable<Claim>? Claims, IEnumerable<string>? Roles);
