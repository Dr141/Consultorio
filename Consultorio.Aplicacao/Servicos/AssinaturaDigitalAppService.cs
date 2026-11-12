using System.Security.Cryptography;
using System.Text;
using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Assinatura digital de documentos com o certificado A1 (ICP-Brasil) do médico.
/// Só o próprio médico (login vinculado ao cadastro) assina; se o médico não tem login vinculado,
/// um administrador pode assinar em seu nome (consultório de um médico só, que também administra o sistema).
/// </summary>
public class AssinaturaDigitalAppService(
    IMedicoRepositorio medicoRepositorio,
    ICertificadoDigitalRepositorio certificadoRepositorio,
    IDocumentoClinicoRepositorio documentoRepositorio,
    IDocumentoAssinaturaRepositorio assinaturaRepositorio,
    ILeitorCertificado leitorCertificado,
    IGeradorPdf geradorPdf,
    IProtetorDados protetor,
    OpcoesClinica opcoes,
    TimeProvider relogio) : IAssinaturaDigitalAppService
{
    private DateTime AgoraUtc => relogio.GetUtcNow().UtcDateTime;

    public async Task<CertificadoDigitalResponse?> ObterCertificadoAsync(int idMedico)
    {
        var certificado = await certificadoRepositorio.ObterAtivoPorMedico(idMedico);
        return certificado is null ? null : Mapear(certificado, AgoraUtc);
    }

    public async Task<CertificadoDigitalResponse> EnviarCertificadoAsync(int idMedico, byte[] pfx, string senha, UsuarioAtual usuario)
    {
        var medico = await medicoRepositorio.ObterId(idMedico)
            ?? throw new DomainException($"Médico com id {idMedico} não encontrado.");
        DomainException.When(!medico.Ativo, "Médico inativo não pode cadastrar certificado.");
        DomainException.When(!usuario.EhAdministrador && !medico.PertenceAoUsuario(usuario.Email),
            "Somente o próprio médico ou um administrador pode cadastrar o certificado digital.");
        DomainException.When(string.IsNullOrEmpty(senha), "Informe a senha do certificado.");

        var dados = leitorCertificado.Ler(pfx, senha);
        var agora = AgoraUtc;
        var novo = new CertificadoDigital(idMedico, dados, protetor.Proteger(pfx), protetor.Proteger(Encoding.UTF8.GetBytes(senha)),
            usuario.Identificacao, agora);

        var atual = await certificadoRepositorio.ObterAtivoPorMedico(idMedico);
        if (atual is not null)
        {
            atual.Desativar(agora);
            await certificadoRepositorio.Atualizar(atual);
        }

        var salvo = await certificadoRepositorio.Adicionar(novo);
        return Mapear(salvo, agora);
    }

    public async Task RemoverCertificadoAsync(int idMedico, UsuarioAtual usuario)
    {
        var medico = await medicoRepositorio.ObterId(idMedico)
            ?? throw new DomainException($"Médico com id {idMedico} não encontrado.");
        DomainException.When(!usuario.EhAdministrador && !medico.PertenceAoUsuario(usuario.Email),
            "Somente o próprio médico ou um administrador pode remover o certificado digital.");

        var atual = await certificadoRepositorio.ObterAtivoPorMedico(idMedico)
            ?? throw new DomainException("O médico não tem certificado digital cadastrado.");
        atual.Desativar(AgoraUtc);
        await certificadoRepositorio.Atualizar(atual);
    }

    public async Task<DocumentoResponse> AssinarDocumentoAsync(int idDocumento, UsuarioAtual usuario)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(idDocumento)
            ?? throw new DomainException($"Documento com id {idDocumento} não encontrado.");
        DomainException.When(documento.Cancelado, "Documento cancelado não pode ser assinado.");
        DomainException.When(documento.Assinado, "O documento já foi assinado digitalmente.");

        var medico = documento.Medico ?? await medicoRepositorio.ObterId(documento.IdMedico);
        var podeAssinar = medico is not null &&
                          (medico.PertenceAoUsuario(usuario.Email) || (usuario.EhAdministrador && medico.EmailUsuario is null));
        DomainException.When(!podeAssinar, "Somente o médico que emitiu o documento pode assiná-lo digitalmente.");

        var agora = AgoraUtc;
        var certificado = await certificadoRepositorio.ObterAtivoPorMedico(documento.IdMedico)
            ?? throw new DomainException("Cadastre o certificado digital do médico (Médicos → Certificado digital) antes de assinar.");
        DomainException.When(!certificado.VigenteEm(agora),
            $"O certificado digital do médico venceu em {certificado.ValidoAte:dd/MM/yyyy}. Cadastre o certificado renovado.");

        byte[] pfx;
        string senha;
        try
        {
            pfx = protetor.Desproteger(certificado.PfxProtegido);
            senha = Encoding.UTF8.GetString(protetor.Desproteger(certificado.SenhaProtegida));
        }
        catch (CryptographicException)
        {
            throw new DomainException(
                "Não foi possível abrir o certificado guardado (as chaves de criptografia do servidor mudaram). Envie o certificado novamente.");
        }

        try
        {
            var resposta = DocumentoAppService.Mapear(documento, opcoes);
            var conteudo = DocumentoPdf.Montar(resposta, opcoes, new InfoAssinaturaPdf(certificado.Titular, agora, certificado.IcpBrasil));
            var motivo = $"{resposta.TipoNome} emitido(a) por {resposta.NomeMedico}, CRM {resposta.CrmMedico}";
            var pdf = geradorPdf.Gerar(conteudo, new CertificadoAssinatura(pfx, senha, motivo, opcoes.Nome));

            // O documento (rastreado) e a assinatura são gravados juntos no mesmo SaveChanges.
            documento.RegistrarAssinatura(certificado.Titular, agora);
            await assinaturaRepositorio.Adicionar(new DocumentoAssinatura(documento.Id, pdf, certificado.Titular, certificado.Thumbprint,
                usuario.Identificacao));
            var atualizado = await documentoRepositorio.Atualizar(documento);
            return DocumentoAppService.Mapear(atualizado, opcoes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }

    public async Task<ArquivoResponse?> ObterPdfAsync(int idDocumento)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(idDocumento);
        return documento is null ? null : await GerarArquivoAsync(documento);
    }

    public async Task<ArquivoResponse?> ObterPdfPortalAsync(int idPaciente, int idDocumento)
    {
        var documento = await documentoRepositorio.ObterDetalhadoPorId(idDocumento);
        if (documento is null || documento.IdPaciente != idPaciente || documento.Cancelado) return null;
        return await GerarArquivoAsync(documento);
    }

    private async Task<ArquivoResponse> GerarArquivoAsync(DocumentoClinico documento)
    {
        var resposta = DocumentoAppService.Mapear(documento, opcoes);

        // Documento cancelado nunca sai com a assinatura: o PDF ganha a marca "CANCELADO".
        if (documento.Assinado && !documento.Cancelado)
        {
            var assinatura = await assinaturaRepositorio.ObterPorDocumento(documento.Id);
            if (assinatura is not null)
            {
                return new ArquivoResponse(DocumentoPdf.NomeArquivo(resposta, assinado: true), "application/pdf", assinatura.Pdf);
            }
        }

        var pdf = geradorPdf.Gerar(DocumentoPdf.Montar(resposta, opcoes));
        return new ArquivoResponse(DocumentoPdf.NomeArquivo(resposta, assinado: false), "application/pdf", pdf);
    }

    private static CertificadoDigitalResponse Mapear(CertificadoDigital c, DateTime agoraUtc)
    {
        var dias = (int)Math.Floor((c.ValidoAte - agoraUtc).TotalDays);
        return new CertificadoDigitalResponse(c.Id, c.IdMedico, c.Titular, c.Emissor, c.NumeroSerie, c.ValidoDe, c.ValidoAte,
            c.IcpBrasil, c.ValidoAte <= agoraUtc, Math.Max(0, dias), c.EnviadoPor, c.DtCriacao);
    }
}
