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
    Task<ConsultaResponse> AtualizarObservacaoAsync(int id, string observacao, UsuarioAtual? usuario = null);
    Task<ConsultaResponse> AtualizarEvolucaoAsync(int id, EvolucaoRequest request, UsuarioAtual? usuario = null);
    Task<ConsultaResponse> FinalizarAsync(int id, UsuarioAtual? usuario = null);
    Task<ConsultaResponse> AnularAsync(int id, string motivo, UsuarioAtual? usuario = null);
    Task<ConsultaResponse> AdicionarAdendoAsync(int id, string texto, UsuarioAtual usuario);
    /// <summary>Registros de prontuário não são excluídos: lança DomainException orientando a anulação.</summary>
    Task RemoverAsync(int id);
}
