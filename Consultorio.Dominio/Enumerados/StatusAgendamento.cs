namespace Consultorio.Dominio.Enumerados;

/// <summary>
/// Situação de um horário já reservado para um paciente. Horários livres não têm status.
/// Fluxo normal: Agendado → Confirmado → Aguardando (chegou) → EmAtendimento → Atendido.
/// Faltou: o paciente não compareceu (marcado pela recepção ou pela rotina automática).
/// </summary>
public enum StatusAgendamento
{
    Agendado = 1,
    Confirmado = 2,
    Aguardando = 3,
    EmAtendimento = 4,
    Atendido = 5,
    Faltou = 6
}
