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
}
