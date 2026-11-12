using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Infraestrutura.Pdf;

/// <summary>Abertura de arquivos .pfx/.p12 (certificado A1) só em memória, sem gravar a chave no sistema.</summary>
public static class CertificadosPfx
{
    public const int TamanhoMaximoBytes = 50 * 1024;

    /// <summary>Lança <see cref="DomainException"/> com mensagem amigável se o arquivo ou a senha forem inválidos.</summary>
    public static X509Certificate2Collection Carregar(byte[] pfx, string? senha)
    {
        DomainException.When(pfx is null || pfx.Length == 0, "Selecione o arquivo do certificado (.pfx ou .p12).");
        DomainException.When(pfx!.Length > TamanhoMaximoBytes, "Arquivo grande demais para um certificado A1 (.pfx ou .p12).");
        try
        {
            return X509CertificateLoader.LoadPkcs12Collection(pfx, senha ?? string.Empty, X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (CryptographicException)
        {
            throw new DomainException("Não foi possível abrir o certificado. Confira a senha e se o arquivo é um certificado A1 (.pfx ou .p12).");
        }
    }

    /// <summary>Certificado do titular: o que tem a chave privada.</summary>
    public static X509Certificate2? Assinante(X509Certificate2Collection colecao) =>
        colecao.FirstOrDefault(c => c.HasPrivateKey);

    public static DadosCertificado Dados(X509Certificate2 certificado)
    {
        // No e-CPF da ICP-Brasil o nome comum vem como "NOME DO TITULAR:CPF"; o CPF não é exibido.
        var nomeComum = certificado.GetNameInfo(X509NameType.SimpleName, forIssuer: false);
        var titular = nomeComum.Split(':')[0].Trim();
        if (string.IsNullOrWhiteSpace(titular)) titular = certificado.Subject;

        var emissor = certificado.GetNameInfo(X509NameType.SimpleName, forIssuer: true);
        if (string.IsNullOrWhiteSpace(emissor)) emissor = certificado.Issuer;

        return new DadosCertificado(
            titular,
            emissor,
            certificado.SerialNumber,
            certificado.GetCertHashString(HashAlgorithmName.SHA256),
            certificado.NotBefore.ToUniversalTime(),
            certificado.NotAfter.ToUniversalTime(),
            certificado.HasPrivateKey,
            EhIcpBrasil(certificado));
    }

    /// <summary>Certificados da ICP-Brasil têm "O=ICP-Brasil" no nome da autoridade emissora.</summary>
    public static bool EhIcpBrasil(X509Certificate2 certificado) =>
        certificado.IssuerName.EnumerateRelativeDistinguishedNames()
            .Where(r => !r.HasMultipleElements)
            .Any(r => r.GetSingleElementType().Value == "2.5.4.10" // O (organização)
                      && string.Equals(r.GetSingleElementValue()?.Trim(), "ICP-Brasil", StringComparison.OrdinalIgnoreCase));
}

public class LeitorCertificado : ILeitorCertificado
{
    public DadosCertificado Ler(byte[] pfx, string senha)
    {
        var colecao = CertificadosPfx.Carregar(pfx, senha);
        try
        {
            var certificado = CertificadosPfx.Assinante(colecao)
                ?? throw new DomainException("O arquivo não contém a chave privada. Exporte o certificado A1 como .pfx/.p12 incluindo a chave privada.");
            return CertificadosPfx.Dados(certificado);
        }
        finally
        {
            foreach (var c in colecao) c.Dispose();
        }
    }
}
