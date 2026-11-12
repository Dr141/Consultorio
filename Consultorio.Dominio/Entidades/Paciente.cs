using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Paciente : Entidade
{
    public int IdPessoa { get; private set; }
    public Pessoa Pessoa { get; private set; } = null!;

    /// <summary>Cadastros não são apagados (o prontuário deve ser guardado por no mínimo 20 anos); são inativados.</summary>
    public bool Ativo { get; private set; } = true;
    public DateTime? DtInativacao { get; private set; }

    // Alertas clínicos exibidos no topo do prontuário e no atendimento (um item por linha).
    public string? Alergias { get; private set; }
    public string? MedicamentosEmUso { get; private set; }
    public string? CondicoesCronicas { get; private set; }

    public void AtualizarAlertas(string? alergias, string? medicamentosEmUso, string? condicoesCronicas)
    {
        Alergias = NormalizarLista(alergias, "Alergias");
        MedicamentosEmUso = NormalizarLista(medicamentosEmUso, "Medicamentos em uso");
        CondicoesCronicas = NormalizarLista(condicoesCronicas, "Condições crônicas");
    }

    private static string? NormalizarLista(string? valor, string nome)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var itens = valor.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        var texto = string.Join('\n', itens);
        DomainException.When(texto.Length > 2000, $"{nome}: no máximo 2000 caracteres.");
        return texto.Length == 0 ? null : texto;
    }

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

    public void Inativar()
    {
        DomainException.When(!Ativo, "O cadastro já está inativo.");
        Ativo = false;
        DtInativacao = DateTime.UtcNow;
    }

    public void Reativar()
    {
        DomainException.When(Ativo, "O cadastro já está ativo.");
        Ativo = true;
        DtInativacao = null;
    }
}
