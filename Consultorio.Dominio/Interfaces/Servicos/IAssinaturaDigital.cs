namespace Consultorio.Dominio.Interfaces.Servicos;

/// <summary>Dados públicos extraídos de um certificado digital (arquivo .pfx/.p12, tipo A1).</summary>
public record DadosCertificado(
    string Titular,
    string Emissor,
    string NumeroSerie,
    string Thumbprint,
    DateTime ValidoDeUtc,
    DateTime ValidoAteUtc,
    bool PossuiChavePrivada,
    bool IcpBrasil);

/// <summary>Certificado usado para assinar: o .pfx e a senha, já desprotegidos, só em memória.</summary>
public sealed record CertificadoAssinatura(byte[] Pfx, string Senha, string Motivo, string? Local);

/// <summary>Bloco de texto do PDF. Estilos: Normal, Negrito, Titulo, Pequeno, Centralizado.</summary>
public record BlocoPdf(string Texto, EstiloBlocoPdf Estilo = EstiloBlocoPdf.Normal, float EspacoAntes = 0);

public enum EstiloBlocoPdf
{
    Normal,
    Negrito,
    Titulo,
    Cabecalho,
    Pequeno,
    Centralizado,
    CentralizadoNegrito,
    Linha
}

/// <summary>Documento a gerar em PDF (A4, retrato), na ordem dos blocos; quebra páginas sozinho.</summary>
public record ConteudoPdf(string Titulo, IReadOnlyList<BlocoPdf> Blocos, string? Rodape = null, string? MarcaDagua = null);

/// <summary>Leitura e validação de certificados A1 (.pfx), sem guardar nada.</summary>
public interface ILeitorCertificado
{
    /// <summary>Lança <see cref="Excecoes.DomainException"/> se o arquivo ou a senha forem inválidos.</summary>
    DadosCertificado Ler(byte[] pfx, string senha);
}

/// <summary>Geração de PDF e, opcionalmente, assinatura digital PAdES (CAdES destacada, SHA-256).</summary>
public interface IGeradorPdf
{
    byte[] Gerar(ConteudoPdf conteudo, CertificadoAssinatura? assinatura = null);
}

/// <summary>Criptografia simétrica de segredos guardados no banco (ex.: .pfx e senha do certificado).</summary>
public interface IProtetorDados
{
    byte[] Proteger(byte[] dados);
    byte[] Desproteger(byte[] dadosProtegidos);
}
