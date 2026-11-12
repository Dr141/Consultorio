using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Valor a receber por um atendimento (particular ou convênio). Não é apagada:
/// a pendente pode ser cancelada e a paga, estornada — sempre com motivo.
/// </summary>
public class Cobranca : Entidade
{
    public const decimal ValorMaximo = 100_000m;

    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;
    public int? IdAgenda { get; private set; }
    public int? IdConvenio { get; private set; }
    public Convenio? Convenio { get; private set; }

    public string Descricao { get; private set; } = string.Empty;
    public decimal Valor { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal ValorFinal => Valor - Desconto;

    public StatusCobranca Status { get; private set; } = StatusCobranca.Pendente;
    public DateTime DtVencimento { get; private set; }
    public FormaPagamento? FormaPagamento { get; private set; }
    public DateTime? DtPagamento { get; private set; }
    public string? RecebidoPor { get; private set; }
    public DateTime? DtCancelamento { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public string RegistradoPor { get; private set; } = string.Empty;

    protected Cobranca() { }

    public Cobranca(int idPaciente, string descricao, decimal valor, DateTime dtVencimento, string registradoPor,
        int? idAgenda = null, int? idConvenio = null)
    {
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        DomainException.When(idAgenda is <= 0, "Atendimento inválido.");
        DomainException.When(idConvenio is <= 0, "Convênio inválido.");
        var desc = (descricao ?? string.Empty).Trim();
        DomainException.When(desc.Length < 2 || desc.Length > 200, "A descrição da cobrança deve ter entre 2 e 200 caracteres.");
        DomainException.When(valor <= 0, "O valor da cobrança deve ser maior que zero.");
        DomainException.When(valor > ValorMaximo, $"O valor não pode ultrapassar {ValorMaximo:N2}.");

        IdPaciente = idPaciente;
        IdAgenda = idAgenda;
        IdConvenio = idConvenio;
        Descricao = desc;
        Valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        DtVencimento = DateTime.SpecifyKind(dtVencimento.Date, DateTimeKind.Utc);
        RegistradoPor = string.IsNullOrWhiteSpace(registradoPor) ? "sistema" : registradoPor.Trim();
    }

    public bool Vencida(DateTime hoje) => Status == StatusCobranca.Pendente && DtVencimento.Date < hoje.Date;

    public void AplicarDesconto(decimal desconto)
    {
        GarantirPendente("receber desconto");
        DomainException.When(desconto < 0, "O desconto não pode ser negativo.");
        DomainException.When(desconto >= Valor, "O desconto deve ser menor que o valor da cobrança.");
        Desconto = Math.Round(desconto, 2, MidpointRounding.AwayFromZero);
    }

    public void RegistrarPagamento(FormaPagamento forma, string recebidoPor, DateTime? dataPagamento = null)
    {
        GarantirPendente("ser paga");
        DomainException.When(!Enum.IsDefined(forma), "Forma de pagamento inválida.");
        DomainException.When(forma == Enumerados.FormaPagamento.Convenio && IdConvenio is null,
            "Somente cobranças de convênio podem ser quitadas pelo convênio.");
        var quando = dataPagamento ?? DateTime.UtcNow;
        DomainException.When(quando > DateTime.UtcNow.AddMinutes(5), "A data do pagamento não pode estar no futuro.");

        Status = StatusCobranca.Paga;
        FormaPagamento = forma;
        DtPagamento = quando.Kind == DateTimeKind.Utc ? quando : DateTime.SpecifyKind(quando, DateTimeKind.Utc);
        RecebidoPor = string.IsNullOrWhiteSpace(recebidoPor) ? "desconhecido" : recebidoPor.Trim();
    }

    public void Cancelar(string motivo)
    {
        GarantirPendente("ser cancelada");
        RegistrarMotivo(motivo);
        Status = StatusCobranca.Cancelada;
    }

    /// <summary>Devolução de um valor já recebido.</summary>
    public void Estornar(string motivo)
    {
        DomainException.When(Status != StatusCobranca.Paga, "Só cobranças pagas podem ser estornadas.");
        RegistrarMotivo(motivo);
        Status = StatusCobranca.Estornada;
    }

    private void RegistrarMotivo(string motivo)
    {
        var texto = (motivo ?? string.Empty).Trim();
        DomainException.When(texto.Length < 5, "Informe o motivo (mínimo de 5 caracteres).");
        DomainException.When(texto.Length > 500, "O motivo não pode ultrapassar 500 caracteres.");
        MotivoCancelamento = texto;
        DtCancelamento = DateTime.UtcNow;
    }

    private void GarantirPendente(string acao) =>
        DomainException.When(Status != StatusCobranca.Pendente, $"Só cobranças pendentes podem {acao}.");
}
