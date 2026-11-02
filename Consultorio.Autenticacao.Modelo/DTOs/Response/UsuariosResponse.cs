using Consultorio.Identity.Modelo.DTOs.Share;

namespace Consultorio.Identity.Modelo.DTOs.Response;

public record UsuariosResponse(IEnumerable<UsuarioDto>? usuarios);
