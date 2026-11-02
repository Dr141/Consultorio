using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IPacienteAppService
{
    Task<List<PacienteResponse>> ObterTodosAsync();
    Task<PacienteResponse?> ObterPorIdAsync(int id);
    Task<PacienteResponse> AdicionarAsync(PacienteRequest request);
    Task<PacienteResponse> AtualizarAsync(int id, PacienteRequest request);
    Task RemoverAsync(int id);
}
