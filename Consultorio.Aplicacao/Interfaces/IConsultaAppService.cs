using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IConsultaAppService
{
    Task<List<ConsultaResponse>> ObterTodosAsync();
    Task<ConsultaResponse?> ObterPorIdAsync(int id);
    Task<List<ConsultaResponse>> ObterPorPacienteAsync(int idPaciente);
    Task<List<ConsultaResponse>> ObterPorMedicoAsync(int idMedico);
    Task<ConsultaResponse> AdicionarAsync(ConsultaRequest request);
    Task<ConsultaResponse> AtualizarObservacaoAsync(int id, string observacao);
    Task RemoverAsync(int id);
}
