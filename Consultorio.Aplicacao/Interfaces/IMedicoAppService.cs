using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IMedicoAppService
{
    Task<List<MedicoResponse>> ObterTodosAsync(bool incluirInativos = false);
    Task<MedicoResponse?> ObterPorIdAsync(int id);
    Task<MedicoResponse> AdicionarAsync(MedicoRequest request);
    Task<MedicoResponse> AtualizarAsync(int id, MedicoRequest request);
    /// <summary>Inativa o cadastro. Médicos com histórico nunca são apagados.</summary>
    Task RemoverAsync(int id);
    Task<MedicoResponse> ReativarAsync(int id);
    /// <summary>Médico vinculado ao e-mail do usuário logado, se houver.</summary>
    Task<MedicoResponse?> ObterPorUsuarioAsync(string email);
}
