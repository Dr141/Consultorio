using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
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
    public StatusExame Status { get; private set; } = StatusExame.Solicitado;
    public DateTime? DtRealizacao { get; private set; }
    public DateTime? DtLaudo { get; private set; }
    public DateTime? DtEntrega { get; private set; }

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
        if (!string.IsNullOrWhiteSpace(Resultado))
        {
            Status = StatusExame.Laudado;
            DtLaudo = DateTime.UtcNow;
        }
    }

    /// <summary>Coleta/execução feita; aguarda resultado.</summary>
    public void RegistrarRealizacao()
    {
        DomainException.When(Cancelado, "Exame cancelado não pode ser marcado como realizado.");
        DomainException.When(Status != StatusExame.Solicitado, "Só exames solicitados podem ser marcados como realizados.");
        Status = StatusExame.Realizado;
        DtRealizacao = DateTime.UtcNow;
    }

    /// <summary>Quando o paciente foi avisado de que o resultado está disponível.</summary>
    public DateTime? AvisoLaudoEnviadoEm { get; private set; }

    public void MarcarAvisoLaudoEnviado() => AvisoLaudoEnviadoEm = DateTime.UtcNow;

    /// <summary>Resultado entregue ao paciente.</summary>
    public void RegistrarEntrega()
    {
        DomainException.When(Cancelado, "Exame cancelado não pode ser entregue.");
        DomainException.When(Status != StatusExame.Laudado, "Só é possível entregar exames com laudo emitido.");
        Status = StatusExame.Entregue;
        DtEntrega = DateTime.UtcNow;
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
        DtRealizacao ??= DateTime.UtcNow;
        DtLaudo = DateTime.UtcNow;
        if (Status != StatusExame.Entregue) Status = StatusExame.Laudado;
    }

    public void Atualizar(string descricao, string? resultado = null, int? idMedico = null)
    {
        DomainException.When(Cancelado, "Exame cancelado não pode ser alterado.");
        Validar(IdPaciente, descricao, idMedico);
        Descricao = descricao.Trim();
        Resultado = resultado?.Trim();
        if (!string.IsNullOrWhiteSpace(Resultado) && Status is StatusExame.Solicitado or StatusExame.Realizado)
        {
            Status = StatusExame.Laudado;
            DtLaudo = DateTime.UtcNow;
        }
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
