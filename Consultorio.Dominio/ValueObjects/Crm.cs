using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

public record Crm
{
    public string Valor { get; }

    public Crm(string valor)
    {
        DomainException.When(string.IsNullOrWhiteSpace(valor), "CRM não pode ser vazio.");
        string trimmed = valor.Trim();
        DomainException.When(trimmed.Length < 2 || trimmed.Length > 20, "CRM deve conter entre 2 e 20 caracteres.");

        Valor = trimmed;
    }

    public static implicit operator string(Crm crm) => crm.Valor;
    public static implicit operator Crm(string valor) => new(valor);

    public override string ToString() => Valor;
}
