using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IRotinasAgendaAppService
{
    /// <summary>Envia lembrete aos pacientes com atendimento nas próximas horas (configurável) que ainda não receberam.</summary>
    Task<ResultadoRotinaResponse> EnviarLembretesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marca como falta os agendamentos sem check-in cujo horário passou da tolerância.</summary>
    Task<int> MarcarFaltasAsync(CancellationToken cancellationToken = default);

    /// <summary>Envio manual do lembrete de um agendamento (botão na fila do dia).</summary>
    Task<bool> EnviarLembreteAsync(int idAgenda, CancellationToken cancellationToken = default);
}
