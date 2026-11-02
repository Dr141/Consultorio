using Consultorio.Modelo.Enumerados;
using System.ComponentModel.DataAnnotations;

namespace Consultorio.Modelo.DTOs.Resquest;

public record MedicoRequest(int IdPessoa, string Crm, IList<Especialidade> Especialidades)
{
    [Required]
    public int IdPessoa { get; init; } = IdPessoa;
    [Required]
    [Length(1, 12, ErrorMessage = "O campo {0} deve ter entre {1} e {2} caracteres")]
    public required string Crm { get; init; } = Crm;
    [Required]
    public IList<Especialidade> Especialidades { get; set; } = Especialidades;
}
