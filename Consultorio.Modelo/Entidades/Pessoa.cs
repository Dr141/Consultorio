using Consultorio.Modelo.Entidades.Base;

namespace Consultorio.Modelo.Entidades;

public class Pessoa : Entidade
{
    public DateTime DtNasc { get; init; }
    public string Cpf { get; init; }
    public string Nome { get; init; }
    public string Telefone { get; init; }
    public string Observacao { get; init; }
    public Medico Medico { get; init; }
    public Paciente Paciente { get; init; }
}
