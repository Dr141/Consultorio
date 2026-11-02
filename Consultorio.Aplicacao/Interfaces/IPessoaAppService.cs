using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IPessoaAppService
{
    Task<List<PessoaResponse>> ObterTodosAsync();
    Task<PessoaResponse?> ObterPorIdAsync(int id);
    Task<PessoaResponse> AdicionarAsync(PessoaRequest request);
    Task<PessoaResponse> AtualizarAsync(int id, PessoaRequest request);
    Task RemoverAsync(int id);
}
