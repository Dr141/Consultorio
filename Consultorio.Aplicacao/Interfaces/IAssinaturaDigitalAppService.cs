using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

/// <summary>Certificado digital A1 dos médicos e assinatura dos documentos clínicos em PDF.</summary>
public interface IAssinaturaDigitalAppService
{
    Task<CertificadoDigitalResponse?> ObterCertificadoAsync(int idMedico);
    Task<CertificadoDigitalResponse> EnviarCertificadoAsync(int idMedico, byte[] pfx, string senha, UsuarioAtual usuario);
    Task RemoverCertificadoAsync(int idMedico, UsuarioAtual usuario);

    Task<DocumentoResponse> AssinarDocumentoAsync(int idDocumento, UsuarioAtual usuario);

    /// <summary>PDF assinado, se houver; senão um PDF sem assinatura (com marca "CANCELADO" se for o caso).</summary>
    Task<ArquivoResponse?> ObterPdfAsync(int idDocumento);

    /// <summary>PDF para o portal: só documentos do próprio paciente e não cancelados.</summary>
    Task<ArquivoResponse?> ObterPdfPortalAsync(int idPaciente, int idDocumento);
}
