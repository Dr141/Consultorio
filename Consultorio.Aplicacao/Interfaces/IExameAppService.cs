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
    Task<ExameResponse> RegistrarResultadoAsync(int id, string resultado, int? idMedico = null);
    Task<ExameResponse> CancelarAsync(int id, string motivo);
    Task<ExameResponse> RegistrarRealizacaoAsync(int id);
    Task<ExameResponse> RegistrarEntregaAsync(int id);
    Task<ExameAnexoResponse> AdicionarAnexoAsync(int id, string nomeArquivo, byte[] conteudo, string enviadoPor);
    Task<List<ExameAnexoResponse>> ObterAnexosAsync(int id);
    Task<ArquivoResponse?> ObterArquivoAnexoAsync(int idExame, int idAnexo);
    /// <summary>Exames não são excluídos: lança DomainException orientando o cancelamento.</summary>
    Task RemoverAsync(int id);
}
