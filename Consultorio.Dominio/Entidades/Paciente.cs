using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Paciente : Entidade
{
    public int IdPessoa { get; private set; }
    public Pessoa Pessoa { get; private set; } = null!;

    private readonly List<Consulta> _consultas = [];
    public IReadOnlyCollection<Consulta> Consultas => _consultas.AsReadOnly();

    private readonly List<Exame> _exames = [];
    public IReadOnlyCollection<Exame> Exames => _exames.AsReadOnly();

    private readonly List<Agenda> _agendas = [];
    public IReadOnlyCollection<Agenda> Agendas => _agendas.AsReadOnly();

    protected Paciente() { }

    public Paciente(int idPessoa)
    {
        DomainException.When(idPessoa <= 0, "Id da pessoa inválido para o paciente.");
        IdPessoa = idPessoa;
    }

    public Paciente(Pessoa pessoa)
    {
        ArgumentNullException.ThrowIfNull(pessoa);
        Pessoa = pessoa;
        IdPessoa = pessoa.Id;
    }

    public void VincularPessoa(Pessoa pessoa)
    {
        ArgumentNullException.ThrowIfNull(pessoa);
        Pessoa = pessoa;
        IdPessoa = pessoa.Id;
    }
}
