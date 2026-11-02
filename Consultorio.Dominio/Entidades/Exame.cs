using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Exame : Entidade
{
    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;

    public int? IdAgenda { get; private set; }
    public Agenda? Agenda { get; private set; }

    public string Descricao { get; private set; } = string.Empty;
    public string? Resultado { get; private set; }

    protected Exame() { }

    public Exame(int idPaciente, string descricao, string? resultado = null, int? idAgenda = null)
    {
        Validar(idPaciente, descricao);

        IdPaciente = idPaciente;
        Descricao = descricao.Trim();
        Resultado = resultado?.Trim();
        IdAgenda = idAgenda;
    }

    public void RegistrarResultado(string resultado)
    {
        DomainException.When(string.IsNullOrWhiteSpace(resultado), "O resultado do exame não pode ser vazio.");
        Resultado = resultado.Trim();
    }

    public void Atualizar(string descricao, string? resultado = null)
    {
        Validar(IdPaciente, descricao);
        Descricao = descricao.Trim();
        Resultado = resultado?.Trim();
    }

    public void VincularAgenda(int idAgenda)
    {
        DomainException.When(idAgenda <= 0, "Id da agenda inválido.");
        IdAgenda = idAgenda;
    }

    private static void Validar(int idPaciente, string descricao)
    {
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        DomainException.When(string.IsNullOrWhiteSpace(descricao), "Descrição do exame é obrigatória.");
        DomainException.When(descricao.Trim().Length < 2 || descricao.Trim().Length > 200, "Descrição deve ter entre 2 e 200 caracteres.");
    }
}
