using Consultorio.Aplicacao.DTOs.Portal;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

/// <summary>Portal do paciente: acesso por código no e-mail e consultas restritas aos dados do próprio paciente.</summary>
public interface IPortalPacienteAppService
{
    Task<PortalSolicitacaoResponse> SolicitarCodigoAsync(PortalSolicitarCodigoRequest request, string? ip, CancellationToken cancellationToken = default);
    Task<PortalSessaoResponse> ValidarCodigoAsync(PortalValidarCodigoRequest request);
    /// <summary>Id do paciente dono da sessão, ou null se o token for inválido/expirado.</summary>
    Task<int?> ValidarSessaoAsync(string? token);
    Task EncerrarSessaoAsync(string? token);

    Task<PortalResumoResponse> ObterResumoAsync(int idPaciente);
    Task<List<PortalMedicoHorariosResponse>> ObterHorariosDisponiveisAsync(int idPaciente);
    Task<PortalAgendamentoResponse> AgendarAsync(int idPaciente, int idAgenda);
    Task<PortalAgendamentoResponse> ConfirmarAsync(int idPaciente, int idAgenda);
    Task CancelarAsync(int idPaciente, int idAgenda);

    Task<List<PortalExameResponse>> ObterExamesAsync(int idPaciente);
    Task<ArquivoResponse?> ObterArquivoExameAsync(int idPaciente, int idExame, int idAnexo);
    Task<List<DocumentoResponse>> ObterDocumentosAsync(int idPaciente);
    Task<DocumentoResponse?> ObterDocumentoAsync(int idPaciente, int idDocumento);
    Task<List<CobrancaResponse>> ObterCobrancasAsync(int idPaciente);
    Task<ReciboResponse?> ObterReciboAsync(int idPaciente, int idCobranca);
}
