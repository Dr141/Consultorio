using System.Text.RegularExpressions;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

/// <summary>Código da Classificação Internacional de Doenças (CID-10), ex.: "J06.9", "I10".</summary>
public record Cid
{
    private static readonly Regex Formato = new(@"^[A-Z][0-9]{2}(\.[0-9A-Z]{1,2})?$", RegexOptions.Compiled);

    public string Codigo { get; }
    public string Descricao { get; }

    public Cid(string codigo, string? descricao)
    {
        var normalizado = (codigo ?? string.Empty).Trim().ToUpperInvariant().Replace(" ", "");
        // Aceita "J069" e converte para "J06.9"
        if (normalizado.Length == 4 && !normalizado.Contains('.')) normalizado = $"{normalizado[..3]}.{normalizado[3]}";

        DomainException.When(!Formato.IsMatch(normalizado), "Código CID-10 inválido. Use o formato letra + dois dígitos, como J06.9 ou I10.");
        DomainException.When((descricao ?? string.Empty).Trim().Length > 200, "A descrição do CID não pode ultrapassar 200 caracteres.");

        Codigo = normalizado;
        Descricao = (descricao ?? string.Empty).Trim();
    }
}
