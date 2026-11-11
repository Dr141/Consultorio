using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Consulta : Entidade
{
    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;

    public int IdMedico { get; private set; }
    public Medico Medico { get; private set; } = null!;

    public int? IdAgenda { get; private set; }
    public Agenda? Agenda { get; private set; }

    public string Observacao { get; private set; } = string.Empty;

    public StatusConsulta Status { get; private set; } = StatusConsulta.Aberta;
    public DateTime? DtFinalizacao { get; private set; }
    public DateTime? DtAnulacao { get; private set; }
    public string? MotivoAnulacao { get; private set; }

    private readonly List<ConsultaAdendo> _adendos = [];
    public IReadOnlyCollection<ConsultaAdendo> Adendos => _adendos.AsReadOnly();

    protected Consulta() { }

    public Consulta(int idPaciente, int idMedico, string observacao, int? idAgenda = null)
    {
        Validar(idPaciente, idMedico, observacao);

        IdPaciente = idPaciente;
        IdMedico = idMedico;
        Observacao = observacao.Trim();
        IdAgenda = idAgenda;
    }

    public void AtualizarObservacao(string observacao)
    {
        DomainException.When(Status == StatusConsulta.Finalizada, "Consulta finalizada não pode ser editada. Registre um adendo.");
        DomainException.When(Status == StatusConsulta.Anulada, "Consulta anulada não pode ser editada.");
        DomainException.When(string.IsNullOrWhiteSpace(observacao), "A observação da consulta não pode ser vazia.");
        DomainException.When(observacao.Trim().Length > 2000, "A observação não pode ultrapassar 2000 caracteres.");

        Observacao = observacao.Trim();
    }

    /// <summary>Encerra o atendimento: a evolução passa a ser imutável.</summary>
    public void Finalizar()
    {
        DomainException.When(Status == StatusConsulta.Finalizada, "A consulta já está finalizada.");
        DomainException.When(Status == StatusConsulta.Anulada, "Consulta anulada não pode ser finalizada.");
        Status = StatusConsulta.Finalizada;
        DtFinalizacao = DateTime.UtcNow;
    }

    /// <summary>Marca o registro como feito por engano. Ele continua no histórico, com o motivo.</summary>
    public void Anular(string motivo)
    {
        DomainException.When(Status == StatusConsulta.Anulada, "A consulta já está anulada.");
        DomainException.When(string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5, "Informe o motivo da anulação (mínimo de 5 caracteres).");
        DomainException.When(motivo.Trim().Length > 500, "O motivo da anulação não pode ultrapassar 500 caracteres.");
        Status = StatusConsulta.Anulada;
        DtAnulacao = DateTime.UtcNow;
        MotivoAnulacao = motivo.Trim();
    }

    public ConsultaAdendo AdicionarAdendo(string texto, string autor)
    {
        DomainException.When(Status == StatusConsulta.Aberta, "Enquanto a consulta estiver aberta, edite a evolução diretamente.");
        DomainException.When(Status == StatusConsulta.Anulada, "Não é possível adicionar adendo a uma consulta anulada.");
        var adendo = new ConsultaAdendo(Id, texto, autor);
        _adendos.Add(adendo);
        return adendo;
    }

    public void VincularAgenda(int idAgenda)
    {
        DomainException.When(idAgenda <= 0, "Id da agenda inválido.");
        IdAgenda = idAgenda;
    }

    private static void Validar(int idPaciente, int idMedico, string observacao)
    {
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        DomainException.When(idMedico <= 0, "Médico inválido.");
        DomainException.When(string.IsNullOrWhiteSpace(observacao), "Observação da consulta é obrigatória.");
        DomainException.When(observacao.Trim().Length > 2000, "A observação não pode ultrapassar 2000 caracteres.");
    }
}
