using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Complemento datado e identificado a uma consulta já finalizada.
/// É a forma correta de corrigir ou complementar o prontuário sem alterar o registro original.
/// </summary>
public class ConsultaAdendo : Entidade
{
    public const int TamanhoMaximoTexto = 2000;

    public int IdConsulta { get; private set; }
    public string Texto { get; private set; } = string.Empty;
    public string Autor { get; private set; } = string.Empty;

    protected ConsultaAdendo() { }

    public ConsultaAdendo(int idConsulta, string texto, string autor)
    {
        DomainException.When(string.IsNullOrWhiteSpace(texto), "O texto do adendo é obrigatório.");
        DomainException.When(texto.Trim().Length > TamanhoMaximoTexto, $"O adendo não pode ultrapassar {TamanhoMaximoTexto} caracteres.");
        DomainException.When(string.IsNullOrWhiteSpace(autor), "O autor do adendo é obrigatório.");

        IdConsulta = idConsulta;
        Texto = texto.Trim();
        Autor = autor.Trim();
    }
}
