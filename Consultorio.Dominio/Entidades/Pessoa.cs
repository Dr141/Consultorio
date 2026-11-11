using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Dominio.Entidades;

public class Pessoa : Entidade
{
    public string Nome { get; private set; } = string.Empty;
    public Cpf Cpf { get; private set; } = null!;
    public DateTime DtNasc { get; private set; }
    public Telefone Telefone { get; private set; } = null!;
    public string? Observacao { get; private set; }

    /// <summary>E-mail de contato (opcional). Usado para lembretes e confirmação de consultas.</summary>
    public string? Email { get; private set; }

    // Propriedades de navegação EF Core
    public Medico? Medico { get; private set; }
    public Paciente? Paciente { get; private set; }

    // Construtor sem parâmetros para ORM
    protected Pessoa() { }

    public Pessoa(string nome, Cpf cpf, DateTime dtNasc, Telefone telefone, string? observacao = null)
    {
        Validar(nome, cpf, dtNasc, telefone, observacao);

        Nome = nome.Trim();
        Cpf = cpf;
        DtNasc = DateTime.SpecifyKind(dtNasc, DateTimeKind.Utc);
        Telefone = telefone;
        Observacao = observacao?.Trim();
    }

    public void Atualizar(string nome, Cpf cpf, DateTime dtNasc, Telefone telefone, string? observacao = null)
    {
        Validar(nome, cpf, dtNasc, telefone, observacao);

        Nome = nome.Trim();
        Cpf = cpf;
        DtNasc = DateTime.SpecifyKind(dtNasc, DateTimeKind.Utc);
        Telefone = telefone;
        Observacao = observacao?.Trim();
    }

    public void DefinirEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            Email = null;
            return;
        }

        var normalizado = email.Trim().ToLowerInvariant();
        DomainException.When(normalizado.Length > 256, "E-mail muito longo.");
        DomainException.When(!System.Text.RegularExpressions.Regex.IsMatch(normalizado, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"), "E-mail inválido.");
        Email = normalizado;
    }

    private static void Validar(string nome, Cpf cpf, DateTime dtNasc, Telefone telefone, string? observacao)
    {
        DomainException.When(string.IsNullOrWhiteSpace(nome), "Nome é obrigatório.");
        DomainException.When(nome.Trim().Length < 2 || nome.Trim().Length > 100, "Nome deve ter entre 2 e 100 caracteres.");
        DomainException.When(cpf is null, "CPF é obrigatório.");
        DomainException.When(telefone is null, "Telefone é obrigatório.");
        DomainException.When(dtNasc > DateTime.UtcNow, "Data de nascimento não pode ser no futuro.");
        DomainException.When(dtNasc < DateTime.UtcNow.AddYears(-130), "Data de nascimento inválida.");
        DomainException.When(observacao is not null && observacao.Length > 500, "Observação não pode exceder 500 caracteres.");
    }
}
