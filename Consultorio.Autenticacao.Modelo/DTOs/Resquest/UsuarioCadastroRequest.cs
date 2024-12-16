using System.ComponentModel.DataAnnotations;

namespace Consultorio.Identity.Modelo.DTOs.Resquest;

public record UsuarioCadastroRequest
{
    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    [EmailAddress(ErrorMessage = "O campo {0} é inválido")]
    public string Email { get; set; }

    [Required(ErrorMessage = "O campo {0} é obrigatório")]
    [StringLength(50, ErrorMessage = "O campo {0} deve ter entre {2} e {1} caracteres", MinimumLength = 6)]
    public string Senha { get; set; }

    [Compare(nameof(Senha), ErrorMessage = "As senhas devem ser iguais")]
    public string SenhaConfirmacao { get; set; }

    public UsuarioCadastroRequest(string email, string senha, string senhaConfirmacao)
    {
        Email = email;
        Senha = senha;
        SenhaConfirmacao = senhaConfirmacao;
    }
}