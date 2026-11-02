using System.Text.RegularExpressions;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

public record Cpf
{
    public string Valor { get; }

    public Cpf(string valor)
    {
        DomainException.When(string.IsNullOrWhiteSpace(valor), "CPF não pode ser vazio.");
        
        string apenasNumeros = Regex.Replace(valor, @"\D", "");
        DomainException.When(apenasNumeros.Length != 11, "CPF deve conter exatamente 11 dígitos.");
        DomainException.When(!ValidarCpf(apenasNumeros), "CPF informado é inválido.");

        Valor = apenasNumeros;
    }

    public static implicit operator string(Cpf cpf) => cpf.Valor;
    public static implicit operator Cpf(string valor) => new(valor);

    public string Formatado()
    {
        return Convert.ToUInt64(Valor).ToString(@"000\.000\.000\-00");
    }

    public override string ToString() => Valor;

    public static bool ValidarCpf(string cpf)
    {
        if (cpf.Length != 11) return false;

        // Rejeita sequências de números iguais
        if (new string(cpf[0], 11) == cpf) return false;

        int[] multiplicador1 = [10, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] multiplicador2 = [11, 10, 9, 8, 7, 6, 5, 4, 3, 2];

        string tempCpf = cpf[..9];
        int soma = 0;

        for (int i = 0; i < 9; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador1[i];

        int resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        string digito = resto.ToString();
        tempCpf += digito;
        soma = 0;

        for (int i = 0; i < 10; i++)
            soma += int.Parse(tempCpf[i].ToString()) * multiplicador2[i];

        resto = soma % 11;
        resto = resto < 2 ? 0 : 11 - resto;

        digito += resto.ToString();

        return cpf.EndsWith(digito);
    }
}
