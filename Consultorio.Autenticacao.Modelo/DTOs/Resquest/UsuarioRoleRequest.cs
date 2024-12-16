using Consultorio.Identity.Modelo.Enumerados;
using System.ComponentModel.DataAnnotations;

namespace Consultorio.Identity.Modelo.DTOs.Resquest;

public record UsuarioRoleRequest
{
    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    [EmailAddress(ErrorMessage = "O campo {0} é inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    public Roles Role { get; set; }

    public UsuarioRoleRequest(string email, Roles role)
    {
        Email = email;
        Role = role;
    }
}