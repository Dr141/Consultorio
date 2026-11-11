using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
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

    public TipoAtendimento Tipo { get; private set; } = TipoAtendimento.Consulta;
    public int DuracaoMinutos { get; private set; } = 30;

    /// <summary>Situação do agendamento; nulo enquanto o horário está livre.</summary>
    public StatusAgendamento? Status { get; private set; }
    public DateTime? DtConfirmacao { get; private set; }
    public DateTime? DtChegada { get; private set; }
    public DateTime? DtInicioAtendimento { get; private set; }
    public DateTime? DtFimAtendimento { get; private set; }

    /// <summary>Token secreto do link de confirmação/desmarcação enviado ao paciente.</summary>
    public Guid? TokenConfirmacao { get; private set; }
    public DateTime? LembreteEnviadoEm { get; private set; }

    protected Agenda() { }

    public Agenda(DateTime data, TimeSpan hora, int? idMedico = null, int? idPaciente = null,
        TipoAtendimento tipo = TipoAtendimento.Consulta, int duracaoMinutos = 30)
    {
        ValidarHorario(data, hora);
        ValidarDuracao(duracaoMinutos);

        Data = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        Hora = hora;
        IdMedico = idMedico;
        Tipo = tipo;
        DuracaoMinutos = duracaoMinutos;

        if (idPaciente.HasValue && idPaciente.Value > 0)
        {
            AgendarPaciente(idPaciente.Value);
        }
    }

    public void AgendarPaciente(int idPaciente)
    {
        DomainException.When(idPaciente <= 0, "Id do paciente inválido.");
        DomainException.When(!Disponivel, "O horário já está ocupado.");

        IdPaciente = idPaciente;
        Status = StatusAgendamento.Agendado;
        TokenConfirmacao = Guid.NewGuid();
        LimparMarcacoes();
    }

    /// <summary>Desmarca o paciente; o horário volta a ficar livre.</summary>
    public void LiberarHorario()
    {
        DomainException.When(Status is StatusAgendamento.EmAtendimento or StatusAgendamento.Atendido,
            "O atendimento deste horário já foi iniciado e não pode ser desmarcado.");

        IdPaciente = null;
        IdConsulta = null;
        IdExame = null;
        Status = null;
        TokenConfirmacao = null;
        LimparMarcacoes();
    }

    public void Confirmar()
    {
        GarantirStatus("confirmar", StatusAgendamento.Agendado, StatusAgendamento.Confirmado);
        if (Status == StatusAgendamento.Confirmado) return;
        Status = StatusAgendamento.Confirmado;
        DtConfirmacao = DateTime.UtcNow;
    }

    /// <summary>Check-in na recepção. Aceita também quem foi marcado como falta e chegou atrasado.</summary>
    public void RegistrarChegada()
    {
        GarantirStatus("registrar a chegada", StatusAgendamento.Agendado, StatusAgendamento.Confirmado, StatusAgendamento.Faltou);
        Status = StatusAgendamento.Aguardando;
        DtChegada = DateTime.UtcNow;
    }

    public void IniciarAtendimento()
    {
        GarantirStatus("iniciar o atendimento", StatusAgendamento.Agendado, StatusAgendamento.Confirmado,
            StatusAgendamento.Aguardando, StatusAgendamento.EmAtendimento);
        if (Status == StatusAgendamento.EmAtendimento) return;
        DtChegada ??= DateTime.UtcNow;
        Status = StatusAgendamento.EmAtendimento;
        DtInicioAtendimento = DateTime.UtcNow;
    }

    public void ConcluirAtendimento()
    {
        GarantirStatus("concluir o atendimento", StatusAgendamento.Aguardando, StatusAgendamento.EmAtendimento, StatusAgendamento.Atendido);
        if (Status == StatusAgendamento.Atendido) return;
        DtInicioAtendimento ??= DateTime.UtcNow;
        Status = StatusAgendamento.Atendido;
        DtFimAtendimento = DateTime.UtcNow;
    }

    public void RegistrarFalta()
    {
        GarantirStatus("registrar a falta", StatusAgendamento.Agendado, StatusAgendamento.Confirmado);
        Status = StatusAgendamento.Faltou;
    }

    public void MarcarLembreteEnviado() => LembreteEnviadoEm = DateTime.UtcNow;

    public void DefinirTipo(TipoAtendimento tipo, int duracaoMinutos)
    {
        ValidarDuracao(duracaoMinutos);
        Tipo = tipo;
        DuracaoMinutos = duracaoMinutos;
    }

    /// <summary>Data e hora do atendimento no horário local da clínica (sem fuso).</summary>
    public DateTime InicioLocal => DateTime.SpecifyKind(Data.Date.Add(Hora), DateTimeKind.Unspecified);

    private void GarantirStatus(string acao, params StatusAgendamento[] permitidos)
    {
        DomainException.When(Status is null, $"Não é possível {acao}: o horário está livre.");
        DomainException.When(!permitidos.Contains(Status!.Value), $"Não é possível {acao} de um agendamento com situação \"{Status}\".");
    }

    private void LimparMarcacoes()
    {
        DtConfirmacao = null;
        DtChegada = null;
        DtInicioAtendimento = null;
        DtFimAtendimento = null;
        LembreteEnviadoEm = null;
    }

    private static void ValidarDuracao(int duracaoMinutos)
    {
        DomainException.When(duracaoMinutos < 5 || duracaoMinutos > 480, "A duração deve ficar entre 5 e 480 minutos.");
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
