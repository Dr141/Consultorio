using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

public class Agenda : Entidade
{
    public DateTime Data { get; private set; }
    public TimeSpan Hora { get; private set; }
    public int? IdMedico { get; private set; }
    public Medico? Medico { get; private set; }
    public int? IdPaciente { get; private set; }
    public Paciente? Paciente { get; private set; }
    public bool Disponivel => IdPaciente is null && IdConsulta is null && IdExame is null;
    public int? IdConsulta { get; private set; }
    public Consulta? Consulta { get; private set; }
    public int? IdExame { get; private set; }
    public Exame? Exame { get; private set; }

    protected Agenda() { }

    public Agenda(DateTime data, TimeSpan hora, int? idMedico = null, int? idPaciente = null)
    {
        ValidarHorario(data, hora);

        Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        Hora = hora;
        IdMedico = idMedico;
        IdPaciente = idPaciente;
    }

    public void AgendarPaciente(int idPaciente)
    {
        DomainException.When(idPaciente <= 0, "Id do paciente inválido.");
        DomainException.When(!Disponivel, "O horário já está ocupado.");

        IdPaciente = idPaciente;
    }

    public void LiberarHorario()
    {
        IdPaciente = null;
        IdConsulta = null;
        IdExame = null;
    }

    public void VincularConsulta(int idConsulta)
    {
        DomainException.When(idConsulta <= 0, "Id da consulta inválido.");
        IdConsulta = idConsulta;
    }

    public void VincularExame(int idExame)
    {
        DomainException.When(idExame <= 0, "Id do exame inválido.");
        IdExame = idExame;
    }

    public void AtualizarHorario(DateTime data, TimeSpan hora, int? idMedico = null)
    {
        ValidarHorario(data, hora);

        Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        Hora = hora;
        if (idMedico.HasValue)
        {
            IdMedico = idMedico.Value;
        }
    }

    private static void ValidarHorario(DateTime data, TimeSpan hora)
    {
        DomainException.When(hora.TotalHours < 0 || hora.TotalHours >= 24, "Horário deve estar entre 00:00 e 23:59.");
    }
}
