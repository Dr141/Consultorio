using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Valor de um tipo de atendimento: particular (sem convênio) ou para um convênio específico.
/// É usado para gerar automaticamente a cobrança quando o atendimento é concluído.
/// </summary>
public class TabelaPreco : Entidade
{
    public const decimal ValorMaximo = 100_000m;

    public TipoAtendimento Tipo { get; private set; }
    public int? IdConvenio { get; private set; }
    public Convenio? Convenio { get; private set; }
    public decimal Valor { get; private set; }
    public DateTime? DtAtualizacao { get; private set; }

    protected TabelaPreco() { }

    public TabelaPreco(TipoAtendimento tipo, int? idConvenio, decimal valor)
    {
        DomainException.When(!Enum.IsDefined(tipo), "Tipo de atendimento inválido.");
        DomainException.When(idConvenio is <= 0, "Convênio inválido.");
        Tipo = tipo;
        IdConvenio = idConvenio;
        DefinirValor(valor);
    }

    public void DefinirValor(decimal valor)
    {
        DomainException.When(valor < 0, "O valor não pode ser negativo.");
        DomainException.When(valor > ValorMaximo, $"O valor não pode ultrapassar {ValorMaximo:N2}.");
        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        DtAtualizacao = DateTime.UtcNow;
    }
}
