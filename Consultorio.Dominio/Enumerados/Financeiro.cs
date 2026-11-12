namespace Consultorio.Dominio.Enumerados;

public enum StatusCobranca
{
    Pendente = 1,
    Paga = 2,
    Cancelada = 3,
    Estornada = 4
}

public enum FormaPagamento
{
    Dinheiro = 1,
    Pix = 2,
    CartaoDebito = 3,
    CartaoCredito = 4,
    Boleto = 5,
    Transferencia = 6,
    /// <summary>Pago pelo convênio (repasse da operadora).</summary>
    Convenio = 7
}

public static class FinanceiroExtensoes
{
    public static string ObterNome(this FormaPagamento forma) => forma switch
    {
        FormaPagamento.Dinheiro => "Dinheiro",
        FormaPagamento.Pix => "Pix",
        FormaPagamento.CartaoDebito => "Cartão de débito",
        FormaPagamento.CartaoCredito => "Cartão de crédito",
        FormaPagamento.Boleto => "Boleto",
        FormaPagamento.Transferencia => "Transferência",
        FormaPagamento.Convenio => "Convênio",
        _ => forma.ToString()
    };

    public static string ObterNome(this StatusCobranca status) => status switch
    {
        StatusCobranca.Pendente => "Pendente",
        StatusCobranca.Paga => "Paga",
        StatusCobranca.Cancelada => "Cancelada",
        StatusCobranca.Estornada => "Estornada",
        _ => status.ToString()
    };
}
