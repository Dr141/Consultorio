using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Período em que não há atendimento (férias, congresso, feriado).
/// Sem médico informado, vale para a clínica inteira.
/// </summary>
public class BloqueioAgenda : Entidade
{
    public int? IdMedico { get; private set; }
    public Medico? Medico { get; private set; }
    public DateTime DataInicio { get; private set; }
    public DateTime DataFim { get; private set; }
    public string Motivo { get; private set; } = string.Empty;

    protected BloqueioAgenda() { }

    public BloqueioAgenda(int? idMedico, DateTime dataInicio, DateTime dataFim, string motivo)
    {
        DomainException.When(idMedico.HasValue && idMedico.Value <= 0, "Médico inválido.");
        DomainException.When(dataFim.Date < dataInicio.Date, "A data final do bloqueio deve ser igual ou posterior à inicial.");
        DomainException.When((dataFim.Date - dataInicio.Date).TotalDays > 366, "O bloqueio pode abranger no máximo um ano.");
        DomainException.When(string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 3, "Informe o motivo do bloqueio.");
        DomainException.When(motivo.Trim().Length > 200, "O motivo do bloqueio não pode ultrapassar 200 caracteres.");

        IdMedico = idMedico;
        DataInicio = DateTime.SpecifyKind(dataInicio.Date, DateTimeKind.Utc);
        DataFim = DateTime.SpecifyKind(dataFim.Date, DateTimeKind.Utc);
        Motivo = motivo.Trim();
    }

    /// <summary>Verdadeiro se o bloqueio impede atendimento do médico (ou de qualquer médico) na data.</summary>
    public bool Abrange(DateTime data, int? idMedico)
    {
        var dia = data.Date;
        var noPeriodo = dia >= DataInicio.Date && dia <= DataFim.Date;
        return noPeriodo && (IdMedico is null || IdMedico == idMedico);
    }
}
