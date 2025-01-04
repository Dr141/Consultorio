using System.ComponentModel.DataAnnotations;

namespace Consultorio.Identity.Modelo.DTOs.Resquest;

public record UsuarioRoleRequest
{
    public UsuarioRoleRequest(string email, IEnumerable<string> roles)
    {
        Email = email;
        Roles = roles;
    }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    [EmailAddress(ErrorMessage = "O campo {0} é inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    public IEnumerable<string> Roles { get; set; }    
}