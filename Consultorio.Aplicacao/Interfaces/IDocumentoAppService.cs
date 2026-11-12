using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IDocumentoAppService
{
    Task<DocumentoResponse> EmitirAsync(DocumentoRequest request, UsuarioAtual usuario);
    Task<DocumentoResponse?> ObterPorIdAsync(int id);
    Task<List<DocumentoResponse>> ObterPorPacienteAsync(int idPaciente);
    Task<DocumentoResponse> CancelarAsync(int id, string motivo, UsuarioAtual usuario);
    /// <summary>Consulta pública de autenticidade pelo código impresso no documento.</summary>
    Task<VerificacaoDocumentoResponse> VerificarAsync(string codigo);
}
