using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

/// <summary>
/// Registro da evolução no formato SOAP, mais um campo livre.
/// S: subjetivo (queixa, história) · O: objetivo (exame físico) · A: avaliação (hipóteses) · P: plano (conduta).
/// Ao menos um dos campos deve ser preenchido.
/// </summary>
public record EvolucaoClinica
{
    public const int TamanhoMaximoCampo = 4000;
    public const int TamanhoMaximoObservacao = 2000;

    public string Observacao { get; }
    public string? Subjetivo { get; }
    public string? Objetivo { get; }
    public string? Avaliacao { get; }
    public string? Plano { get; }

    public EvolucaoClinica(string? observacao, string? subjetivo = null, string? objetivo = null, string? avaliacao = null, string? plano = null)
    {
        Observacao = observacao?.Trim() ?? string.Empty;
        Subjetivo = Normalizar(subjetivo);
        Objetivo = Normalizar(objetivo);
        Avaliacao = Normalizar(avaliacao);
        Plano = Normalizar(plano);

        DomainException.When(Observacao.Length == 0 && Subjetivo is null && Objetivo is null && Avaliacao is null && Plano is null,
            "Registre a evolução da consulta (ao menos um campo).");
        DomainException.When(Observacao.Length > TamanhoMaximoObservacao, $"A observação não pode ultrapassar {TamanhoMaximoObservacao} caracteres.");
        foreach (var (campo, nome) in new[] { (Subjetivo, "Subjetivo"), (Objetivo, "Objetivo"), (Avaliacao, "Avaliação"), (Plano, "Plano") })
        {
            DomainException.When(campo is not null && campo.Length > TamanhoMaximoCampo, $"O campo {nome} não pode ultrapassar {TamanhoMaximoCampo} caracteres.");
        }
    }

    private static string? Normalizar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
