using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Pessoa : Entidade
{
    public DateTime DtNasc { get; init; }
    public required string Cpf { get; init; }
    public required string Nome { get; init; }
    public required string Telefone { get; init; }
    public string? Observacao { get; init; }
    public Medico? Medico { get; set; }
    public Paciente? Paciente { get; set; }
}
