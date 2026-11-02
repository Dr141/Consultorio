using Consultorio.Dominio.Entidades.Base;
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
        DomainException.When(string.IsNullOrWhiteSpace(observacao), "A observação da consulta não pode ser vazia.");
        DomainException.When(observacao.Trim().Length > 2000, "A observação não pode ultrapassar 2000 caracteres.");

        Observacao = observacao.Trim();
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
