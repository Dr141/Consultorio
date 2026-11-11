using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IAgendaAppService
{
    Task<List<AgendaResponse>> ObterTodosAsync();
    Task<AgendaResponse?> ObterPorIdAsync(int id);
    Task<List<AgendaResponse>> ObterPorMedicoEDataAsync(int idMedico, DateTime data);
    Task<List<AgendaResponse>> ObterPorPacienteAsync(int idPaciente);
    Task<AgendaResponse> AdicionarAsync(AgendaRequest request);
    Task<AgendaResponse> AtualizarAsync(int id, AgendaRequest request);
    Task<AgendaResponse> AgendarPacienteAsync(int id, int idPaciente);
    Task<AgendaResponse> LiberarHorarioAsync(int id);
    Task RemoverAsync(int id);

    /// <summary>Agendamentos (horários com paciente) de um dia, para a fila da recepção.</summary>
    Task<List<AgendaResponse>> ObterDoDiaAsync(DateTime data, int? idMedico = null);
    Task<AgendaResponse> ConfirmarAsync(int id);
    Task<AgendaResponse> RegistrarChegadaAsync(int id);
    Task<AgendaResponse> IniciarAtendimentoAsync(int id);
    Task<AgendaResponse> ConcluirAtendimentoAsync(int id);
    Task<AgendaResponse> RegistrarFaltaAsync(int id);

    Task<GradeResponse> GerarGradeAsync(GradeRequest request);
    Task<List<BloqueioResponse>> ObterBloqueiosAsync();
    Task<BloqueioCriadoResponse> CriarBloqueioAsync(BloqueioRequest request);
    Task RemoverBloqueioAsync(int id);

    Task<AgendaResponse?> ObterPorTokenAsync(Guid token);

    /// <summary>Resposta do paciente pelo link do lembrete (sem login).</summary>
    Task<ConfirmacaoPacienteResponse> ResponderConfirmacaoAsync(Guid token, bool confirmar);
}
