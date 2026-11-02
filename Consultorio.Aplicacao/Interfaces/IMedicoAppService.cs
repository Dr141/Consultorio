using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IMedicoAppService
{
    Task<List<MedicoResponse>> ObterTodosAsync();
    Task<MedicoResponse?> ObterPorIdAsync(int id);
    Task<MedicoResponse> AdicionarAsync(MedicoRequest request);
    Task<MedicoResponse> AtualizarAsync(int id, MedicoRequest request);
    Task RemoverAsync(int id);
}
