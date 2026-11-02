using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IExameAppService
{
    Task<List<ExameResponse>> ObterTodosAsync();
    Task<ExameResponse?> ObterPorIdAsync(int id);
    Task<List<ExameResponse>> ObterPorPacienteAsync(int idPaciente);
    Task<ExameResponse> AdicionarAsync(ExameRequest request);
    Task<ExameResponse> AtualizarAsync(int id, ExameRequest request);
    Task<ExameResponse> RegistrarResultadoAsync(int id, string resultado);
    Task RemoverAsync(int id);
}
