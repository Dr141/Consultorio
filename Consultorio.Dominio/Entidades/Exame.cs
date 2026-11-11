using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Exame : Entidade
{
    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;

    public int? IdAgenda { get; private set; }
    public Agenda? Agenda { get; private set; }

    public int? IdMedico { get; private set; }
    public Medico? Medico { get; private set; }

    public string Descricao { get; private set; } = string.Empty;
    public string? Resultado { get; private set; }

    /// <summary>Exames não são apagados; solicitações feitas por engano são canceladas com motivo.</summary>
    public bool Cancelado { get; private set; }
    public DateTime? DtCancelamento { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    protected Exame() { }

    public Exame(int idPaciente, string descricao, string? resultado = null, int? idAgenda = null, int? idMedico = null)
    {
        Validar(idPaciente, descricao, idMedico);

        IdPaciente = idPaciente;
        Descricao = descricao.Trim();
        Resultado = resultado?.Trim();
        IdAgenda = idAgenda;
        IdMedico = idMedico;
    }

    public void Cancelar(string motivo)
    {
        DomainException.When(Cancelado, "O exame já está cancelado.");
        DomainException.When(!string.IsNullOrWhiteSpace(Resultado), "Exame com laudo emitido não pode ser cancelado.");
        DomainException.When(string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5, "Informe o motivo do cancelamento (mínimo de 5 caracteres).");
        DomainException.When(motivo.Trim().Length > 500, "O motivo do cancelamento não pode ultrapassar 500 caracteres.");
        Cancelado = true;
        DtCancelamento = DateTime.UtcNow;
        MotivoCancelamento = motivo.Trim();
    }

    public void RegistrarResultado(string resultado, int? idMedico = null)
    {
        DomainException.When(Cancelado, "Exame cancelado não pode receber laudo.");
        DomainException.When(string.IsNullOrWhiteSpace(resultado), "O resultado do exame não pode ser vazio.");
        if (idMedico.HasValue)
        {
            DomainException.When(idMedico.Value <= 0, "Médico inválido.");
            IdMedico = idMedico.Value;
        }
        Resultado = resultado.Trim();
    }

    public void Atualizar(string descricao, string? resultado = null, int? idMedico = null)
    {
        DomainException.When(Cancelado, "Exame cancelado não pode ser alterado.");
        Validar(IdPaciente, descricao, idMedico);
        Descricao = descricao.Trim();
        Resultado = resultado?.Trim();
        if (idMedico.HasValue)
        {
            IdMedico = idMedico.Value;
        }
    }

    public void VincularAgenda(int idAgenda)
    {
        DomainException.When(idAgenda <= 0, "Id da agenda inválido.");
        IdAgenda = idAgenda;
    }

    public void VincularMedico(int idMedico)
    {
        DomainException.When(idMedico <= 0, "Id do médico inválido.");
        IdMedico = idMedico;
    }

    private static void Validar(int idPaciente, string descricao, int? idMedico = null)
    {
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        DomainException.When(idMedico.HasValue && idMedico.Value <= 0, "Médico inválido.");
        DomainException.When(string.IsNullOrWhiteSpace(descricao), "Descrição do exame é obrigatória.");
        DomainException.When(descricao.Trim().Length < 2 || descricao.Trim().Length > 200, "Descrição deve ter entre 2 e 200 caracteres.");
    }
}
