using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Dominio.Entidades;

public class Medico : Entidade
{
    public int IdPessoa { get; private set; }
    public Pessoa Pessoa { get; private set; } = null!;
    public Crm Crm { get; private set; } = null!;

    private List<Especialidade> _especialidades = [];
    public IReadOnlyCollection<Especialidade> Especialidades
    {
        get => _especialidades.AsReadOnly();
        private set => _especialidades = value?.ToList() ?? [];
    }

    private readonly List<Consulta> _consultas = [];
    public IReadOnlyCollection<Consulta> Consultas => _consultas.AsReadOnly();

    private readonly List<Exame> _exames = [];
    public IReadOnlyCollection<Exame> Exames => _exames.AsReadOnly();

    private readonly List<Agenda> _agendas = [];
    public IReadOnlyCollection<Agenda> Agendas => _agendas.AsReadOnly();

    protected Medico() { }

    public Medico(int idPessoa, Crm crm, IEnumerable<Especialidade> especialidades)
    {
        DomainException.When(idPessoa <= 0, "Id da pessoa inválido para o médico.");
        ArgumentNullException.ThrowIfNull(crm);

        var listaEsp = especialidades?.Distinct().ToList() ?? [];
        DomainException.When(listaEsp.Count == 0, "Médico deve possuir ao menos uma especialidade.");

        IdPessoa = idPessoa;
        Crm = crm;
        _especialidades.AddRange(listaEsp);
    }

    public Medico(Pessoa pessoa, Crm crm, IEnumerable<Especialidade> especialidades)
    {
        ArgumentNullException.ThrowIfNull(pessoa);
        ArgumentNullException.ThrowIfNull(crm);
        var listaEsp = especialidades?.Distinct().ToList() ?? [];
        DomainException.When(listaEsp.Count == 0, "Médico deve possuir ao menos uma especialidade.");

        Pessoa = pessoa;
        IdPessoa = pessoa.Id;
        Crm = crm;
        _especialidades.AddRange(listaEsp);
    }

    public void Atualizar(Crm crm, IEnumerable<Especialidade> especialidades)
    {
        ArgumentNullException.ThrowIfNull(crm);
        var listaEsp = especialidades?.Distinct().ToList() ?? [];
        DomainException.When(listaEsp.Count == 0, "Médico deve possuir ao menos uma especialidade.");

        Crm = crm;
        _especialidades.Clear();
        _especialidades.AddRange(listaEsp);
    }

    public void AdicionarEspecialidade(Especialidade especialidade)
    {
        if (!_especialidades.Contains(especialidade))
        {
            _especialidades.Add(especialidade);
        }
    }

    public void RemoverEspecialidade(Especialidade especialidade)
    {
        DomainException.When(_especialidades.Count <= 1, "Médico não pode ficar sem nenhuma especialidade.");
        _especialidades.Remove(especialidade);
    }
}
