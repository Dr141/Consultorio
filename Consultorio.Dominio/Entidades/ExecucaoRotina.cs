using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Controle das rotinas que rodam uma vez por período (resumo diário, indicadores mensais...),
/// para não repetir o envio quando o servidor reinicia.
/// </summary>
public class ExecucaoRotina : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    /// <summary>Período já processado, ex.: "2026-11-11" (diária) ou "2026-11" (mensal).</summary>
    public string Referencia { get; private set; } = string.Empty;
    public DateTime DtExecucao { get; private set; }

    protected ExecucaoRotina() { }

    public ExecucaoRotina(string nome, string referencia)
    {
        DomainException.When(string.IsNullOrWhiteSpace(nome) || nome.Length > 100, "Nome de rotina inválido.");
        Nome = nome.Trim();
        Registrar(referencia);
    }

    public bool JaExecutou(string referencia) => Referencia == referencia;

    public void Registrar(string referencia)
    {
        DomainException.When(string.IsNullOrWhiteSpace(referencia) || referencia.Length > 20, "Referência de rotina inválida.");
        Referencia = referencia.Trim();
        DtExecucao = DateTime.UtcNow;
    }
}
