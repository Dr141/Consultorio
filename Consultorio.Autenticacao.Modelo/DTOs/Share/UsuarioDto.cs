using System.Security.Claims;

public record UsuarioDto(string Email, Boolean EmailConfirmado, IEnumerable<Claim>? Claims, IEnumerable<string>? Roles);
