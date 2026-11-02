using System.Text.RegularExpressions;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

public record Telefone
{
    public string Valor { get; }

    public Telefone(string valor)
    {
        DomainException.When(string.IsNullOrWhiteSpace(valor), "Telefone não pode ser vazio.");

        string apenasNumeros = Regex.Replace(valor, @"\D", "");
        DomainException.When(apenasNumeros.Length < 10 || apenasNumeros.Length > 11, "Telefone deve conter 10 ou 11 dígitos numéricos com DDD.");

        Valor = apenasNumeros;
    }

    public static implicit operator string(Telefone tel) => tel.Valor;
    public static implicit operator Telefone(string valor) => new(valor);

    public string Formatado()
    {
        if (Valor.Length == 11)
            return Convert.ToUInt64(Valor).ToString(@"(00) 00000-0000");

        return Convert.ToUInt64(Valor).ToString(@"(00) 0000-0000");
    }

    public override string ToString() => Valor;
}
