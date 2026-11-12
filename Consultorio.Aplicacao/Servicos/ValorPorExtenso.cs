namespace Consultorio.Aplicacao.Servicos;

/// <summary>Valor monetário por extenso em português (usado no recibo). Ex.: 150,50 → "cento e cinquenta reais e cinquenta centavos".</summary>
public static class ValorPorExtenso
{
    private static readonly string[] Unidades = ["", "um", "dois", "três", "quatro", "cinco", "seis", "sete", "oito", "nove",
        "dez", "onze", "doze", "treze", "quatorze", "quinze", "dezesseis", "dezessete", "dezoito", "dezenove"];
    private static readonly string[] Dezenas = ["", "", "vinte", "trinta", "quarenta", "cinquenta", "sessenta", "setenta", "oitenta", "noventa"];
    private static readonly string[] Centenas = ["", "cento", "duzentos", "trezentos", "quatrocentos", "quinhentos", "seiscentos", "setecentos", "oitocentos", "novecentos"];

    public static string Converter(decimal valor)
    {
        if (valor < 0) return "menos " + Converter(-valor);
        valor = Math.Round(valor, 2, MidpointRounding.AwayFromZero);
        var reais = (long)Math.Floor(valor);
        var centavos = (int)Math.Round((valor - reais) * 100);

        if (reais == 0 && centavos == 0) return "zero real";

        var partes = new List<string>();
        if (reais > 0)
        {
            var milhoes = reais >= 1_000_000 && reais % 1_000_000 == 0;
            partes.Add(Inteiro(reais) + (reais == 1 ? " real" : milhoes ? " de reais" : " reais"));
        }
        if (centavos > 0)
        {
            partes.Add(Inteiro(centavos) + (centavos == 1 ? " centavo" : " centavos"));
        }
        return string.Join(" e ", partes);
    }

    private static string Inteiro(long numero)
    {
        if (numero == 0) return "zero";
        var grupos = new List<(long valor, string singular, string plural)>
        {
            (numero / 1_000_000_000 % 1000, "bilhão", "bilhões"),
            (numero / 1_000_000 % 1000, "milhão", "milhões"),
            (numero / 1000 % 1000, "mil", "mil"),
            (numero % 1000, "", "")
        };

        var termos = new List<string>();
        foreach (var (valor, singular, plural) in grupos)
        {
            if (valor == 0) continue;
            string texto;
            if (singular == "mil")
                texto = valor == 1 ? "mil" : $"{Centena((int)valor)} mil";
            else if (singular.Length > 0)
                texto = $"{Centena((int)valor)} {(valor == 1 ? singular : plural)}";
            else
                texto = Centena((int)valor);
            termos.Add(texto);
        }

        if (termos.Count == 1) return termos[0];

        // "e" antes do último grupo quando ele é menor que 100 ou uma centena exata (ex.: "mil e quinhentos", "dois mil e cinco");
        // nos demais casos, sem conector ("mil quinhentos e cinquenta").
        var ultimo = numero % 1000;
        if (ultimo == 0) return string.Join(" e ", termos);
        var conector = ultimo < 100 || ultimo % 100 == 0 ? " e " : " ";
        return string.Join(" e ", termos.Take(termos.Count - 1)) + conector + termos[^1];
    }

    private static string Centena(int n)
    {
        if (n == 100) return "cem";
        var c = n / 100;
        var resto = n % 100;
        var partes = new List<string>();
        if (c > 0) partes.Add(Centenas[c]);
        if (resto > 0)
        {
            if (resto < 20) partes.Add(Unidades[resto]);
            else
            {
                var d = resto / 10;
                var u = resto % 10;
                partes.Add(u == 0 ? Dezenas[d] : $"{Dezenas[d]} e {Unidades[u]}");
            }
        }
        return string.Join(" e ", partes);
    }
}
