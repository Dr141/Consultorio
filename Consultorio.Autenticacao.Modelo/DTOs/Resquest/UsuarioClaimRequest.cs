using System.ComponentModel.DataAnnotations;

namespace Consultorio.Identity.Modelo.DTOs.Resquest;

public record UsuarioClaimRequest
{
    public UsuarioClaimRequest(string email, IDictionary<string, string> claims)
    {
        Email = email;
        Claims = claims;
    }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    [EmailAddress(ErrorMessage = "O campo {0} é inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    public IDictionary<string, string> Claims { get; set; }    
}