using System.Text.RegularExpressions;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>Operadora de plano de saúde atendida pela clínica.</summary>
public class Convenio : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    /// <summary>Registro da operadora na ANS (6 dígitos), opcional.</summary>
    public string? RegistroAns { get; private set; }
    public string? Telefone { get; private set; }
    public string? Observacao { get; private set; }
    public bool Ativo { get; private set; } = true;

    protected Convenio() { }

    public Convenio(string nome, string? registroAns = null, string? telefone = null, string? observacao = null)
    {
        Atualizar(nome, registroAns, telefone, observacao);
    }

    public void Atualizar(string nome, string? registroAns, string? telefone, string? observacao)
    {
        var nomeLimpo = (nome ?? string.Empty).Trim();
        DomainException.When(nomeLimpo.Length < 2 || nomeLimpo.Length > 120, "O nome do convênio deve ter entre 2 e 120 caracteres.");

        var ans = Regex.Replace(registroAns ?? string.Empty, @"\D", "");
        DomainException.When(ans.Length > 0 && ans.Length != 6, "O registro ANS deve ter 6 dígitos.");

        var tel = (telefone ?? string.Empty).Trim();
        DomainException.When(tel.Length > 30, "Telefone do convênio muito longo.");
        var obs = (observacao ?? string.Empty).Trim();
        DomainException.When(obs.Length > 500, "A observação do convênio não pode ultrapassar 500 caracteres.");

        Nome = nomeLimpo;
        RegistroAns = ans.Length == 0 ? null : ans;
        Telefone = tel.Length == 0 ? null : tel;
        Observacao = obs.Length == 0 ? null : obs;
    }

    public void Inativar()
    {
        DomainException.When(!Ativo, "O convênio já está inativo.");
        Ativo = false;
    }

    public void Reativar()
    {
        DomainException.When(Ativo, "O convênio já está ativo.");
        Ativo = true;
    }
}
