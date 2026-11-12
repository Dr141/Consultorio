using System.Formats.Asn1;
using System.Globalization;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Infraestrutura.Pdf;

/// <summary>
/// Gerador de PDF simples (A4, fontes padrão Helvetica, sem dependências externas) com assinatura
/// digital PAdES: o arquivo já nasce com o campo de assinatura e um espaço reservado; os bytes do
/// arquivo (menos esse espaço) são assinados em CMS/CAdES destacado com SHA-256 e o resultado é
/// gravado no espaço. Leitores como Adobe Acrobat e o Validador ITI (validar.iti.gov.br) reconhecem a assinatura.
/// </summary>
public class GeradorPdf : IGeradorPdf
{
    private const float LarguraPagina = 595f;
    private const float AlturaPagina = 842f;
    private const float Margem = 56f;
    private const float Topo = 60f;
    private const float Base = 78f;
    private const int TamanhoAssinaturaHex = 32768; // 16 KB para o CMS (certificado + cadeia + assinatura)
    private const int TamanhoByteRange = 64;

    private static readonly CultureInfo Invariante = CultureInfo.InvariantCulture;

    public byte[] Gerar(ConteudoPdf conteudo, CertificadoAssinatura? assinatura = null)
    {
        ArgumentNullException.ThrowIfNull(conteudo);
        var paginas = Paginar(conteudo);

        if (assinatura is null) return Montar(conteudo, paginas, null).Bytes;

        var colecao = CertificadosPfx.Carregar(assinatura.Pfx, assinatura.Senha);
        try
        {
            var certificado = CertificadosPfx.Assinante(colecao)
                ?? throw new DomainException("O certificado não contém a chave privada.");
            var agora = DateTime.UtcNow;
            DomainException.When(certificado.NotAfter.ToUniversalTime() <= agora, "O certificado digital está vencido. Envie um certificado válido.");

            var titular = CertificadosPfx.Dados(certificado).Titular;
            var pdf = Montar(conteudo, paginas, new DadosAssinaturaPdf(titular, assinatura.Motivo, assinatura.Local, agora));
            return Assinar(pdf, certificado, colecao);
        }
        finally
        {
            foreach (var c in colecao) c.Dispose();
        }
    }

    // ------------------------------------------------------------------ layout

    private sealed record Linha(string Texto, string Fonte, float Tamanho, float X, float Y, float Cinza, bool Regua = false);

    private sealed record Estilo(string Fonte, float Tamanho, float Entrelinha, bool Centralizado, float Cinza);

    private static Estilo ObterEstilo(EstiloBlocoPdf estilo) => estilo switch
    {
        EstiloBlocoPdf.Cabecalho => new("F2", 16, 1.3f, false, 0),
        EstiloBlocoPdf.Titulo => new("F2", 14, 1.4f, true, 0),
        EstiloBlocoPdf.Negrito => new("F2", 11, 1.45f, false, 0),
        EstiloBlocoPdf.Pequeno => new("F1", 9, 1.35f, false, 0.35f),
        EstiloBlocoPdf.Centralizado => new("F1", 10, 1.35f, true, 0),
        EstiloBlocoPdf.CentralizadoNegrito => new("F2", 11, 1.35f, true, 0),
        _ => new("F1", 11, 1.45f, false, 0)
    };

    private static List<List<Linha>> Paginar(ConteudoPdf conteudo)
    {
        var paginas = new List<List<Linha>> { new() };
        var y = AlturaPagina - Topo;
        var larguraUtil = LarguraPagina - 2 * Margem;

        foreach (var bloco in conteudo.Blocos)
        {
            y -= bloco.EspacoAntes;
            if (bloco.Estilo == EstiloBlocoPdf.Linha)
            {
                if (y - 12 < Base) { paginas.Add([]); y = AlturaPagina - Topo; }
                var largura = string.IsNullOrEmpty(bloco.Texto) ? larguraUtil : Math.Min(larguraUtil, float.Parse(bloco.Texto, Invariante));
                // Régua (ex.: linha de assinatura): o texto do bloco é a largura em pontos; Tamanho guarda a largura.
                paginas[^1].Add(new Linha(string.Empty, "F1", largura, (LarguraPagina - largura) / 2, y, 0, Regua: true));
                y -= 12;
                continue;
            }

            var estilo = ObterEstilo(bloco.Estilo);
            var altura = estilo.Tamanho * estilo.Entrelinha;
            var texto = bloco.Estilo == EstiloBlocoPdf.Titulo ? bloco.Texto.ToUpperInvariant() : bloco.Texto;
            foreach (var paragrafo in Sanitizar(texto).Split('\n'))
            {
                var linhas = Quebrar(paragrafo, estilo.Fonte == "F2", estilo.Tamanho, larguraUtil);
                if (linhas.Count == 0) linhas.Add(string.Empty);
                foreach (var linha in linhas)
                {
                    if (y - altura < Base) { paginas.Add([]); y = AlturaPagina - Topo; }
                    y -= altura;
                    var largura = Largura(linha, estilo.Fonte == "F2", estilo.Tamanho);
                    var x = estilo.Centralizado ? (LarguraPagina - largura) / 2 : Margem;
                    if (linha.Length > 0) paginas[^1].Add(new Linha(linha, estilo.Fonte, estilo.Tamanho, x, y + altura * 0.25f, estilo.Cinza));
                }
            }
        }

        return paginas;
    }

    /// <summary>Quebra o parágrafo em linhas que cabem na largura (palavras longas demais são cortadas).</summary>
    private static List<string> Quebrar(string paragrafo, bool negrito, float tamanho, float larguraMaxima)
    {
        var linhas = new List<string>();
        var atual = new StringBuilder();
        foreach (var palavra in paragrafo.Split(' '))
        {
            var candidata = atual.Length == 0 ? palavra : atual + " " + palavra;
            if (Largura(candidata, negrito, tamanho) <= larguraMaxima)
            {
                atual.Clear().Append(candidata);
                continue;
            }

            if (atual.Length > 0) linhas.Add(atual.ToString());
            atual.Clear();
            var resto = palavra;
            while (Largura(resto, negrito, tamanho) > larguraMaxima && resto.Length > 1)
            {
                var corte = resto.Length - 1;
                while (corte > 1 && Largura(resto[..corte], negrito, tamanho) > larguraMaxima) corte--;
                linhas.Add(resto[..corte]);
                resto = resto[corte..];
            }
            atual.Append(resto);
        }
        if (atual.Length > 0) linhas.Add(atual.ToString());
        return linhas;
    }

    // Larguras (1/1000 em) das fontes padrão Helvetica e Helvetica-Bold, caracteres 32 a 126.
    private static readonly short[] LargurasNormal =
    [
        278, 278, 355, 556, 556, 889, 667, 191, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 278, 278, 584, 584, 584, 556,
        1015, 667, 667, 722, 722, 667, 611, 778, 722, 278, 500, 667, 556, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 278, 278, 278, 469, 556,
        333, 556, 556, 500, 556, 556, 278, 556, 556, 222, 222, 500, 222, 833, 556, 556,
        556, 556, 333, 500, 278, 556, 500, 722, 500, 500, 500, 334, 260, 334, 584
    ];

    private static readonly short[] LargurasNegrito =
    [
        278, 333, 474, 556, 556, 889, 722, 238, 333, 333, 389, 584, 278, 333, 278, 278,
        556, 556, 556, 556, 556, 556, 556, 556, 556, 556, 333, 333, 584, 584, 584, 611,
        975, 722, 722, 722, 722, 667, 611, 778, 722, 278, 556, 722, 611, 833, 722, 778,
        667, 778, 722, 667, 611, 722, 667, 944, 667, 667, 611, 333, 278, 333, 584, 556,
        333, 556, 611, 556, 611, 556, 333, 611, 611, 278, 278, 556, 278, 889, 611, 611,
        611, 611, 389, 556, 333, 611, 556, 778, 556, 556, 500, 389, 280, 389, 584
    ];

    private static float Largura(string texto, bool negrito, float tamanho)
    {
        var tabela = negrito ? LargurasNegrito : LargurasNormal;
        var total = 0;
        foreach (var c in texto)
        {
            var baseChar = c;
            if (c > 126)
            {
                // Letras acentuadas têm a largura da letra base (á → a, Ç → C).
                var decomposto = c.ToString().Normalize(NormalizationForm.FormD);
                baseChar = decomposto.Length > 0 && decomposto[0] is >= ' ' and <= '~' ? decomposto[0] : 'W';
            }
            total += baseChar is >= ' ' and <= '~' ? tabela[baseChar - 32] : 556;
        }
        return total * tamanho / 1000f;
    }

    /// <summary>Mantém só caracteres que existem na codificação WinAnsi das fontes padrão.</summary>
    public static string Sanitizar(string? texto)
    {
        var sb = new StringBuilder((texto ?? string.Empty).Length);
        foreach (var c in (texto ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n'))
        {
            switch (c)
            {
                case '\n': sb.Append('\n'); break;
                case '\t': sb.Append("    "); break;
                case '—' or '–' or '‒' or '−': sb.Append('-'); break;
                case '•' or '◦' or '▪': sb.Append('-'); break;
                case '“' or '”' or '„': sb.Append('"'); break;
                case '‘' or '’' or '‚': sb.Append('\''); break;
                case '…': sb.Append("..."); break;
                case '₀' or '₁' or '₂' or '₃' or '₄' or '₅' or '₆' or '₇' or '₈' or '₉': sb.Append((char)('0' + (c - '₀'))); break;
                case '\u00A0': sb.Append(' '); break;
                default:
                    if (c < ' ') sb.Append(' ');
                    else if (c is >= '\u0080' and <= '\u009F' || c > '\u00FF') sb.Append('?');
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }

    // ------------------------------------------------------------------ escrita do arquivo

    private sealed record DadosAssinaturaPdf(string Titular, string Motivo, string? Local, DateTime DataUtc);

    private sealed record PdfMontado(byte[] Bytes, int PosicaoByteRange, int InicioContents, int FimContents);

    private static PdfMontado Montar(ConteudoPdf conteudo, List<List<Linha>> paginas, DadosAssinaturaPdf? assinatura)
    {
        var assinar = assinatura is not null;
        var primeiraPagina = assinar ? 8 : 6;
        var totalObjetos = primeiraPagina + paginas.Count * 2;
        var offsets = new long[totalObjetos];
        var ms = new MemoryStream();
        int posByteRange = -1, inicioContents = -1, fimContents = -1;

        void Escrever(string s) { var b = Encoding.Latin1.GetBytes(s); ms.Write(b, 0, b.Length); }
        void Abrir(int numero) { offsets[numero] = ms.Position; Escrever($"{numero} 0 obj\n"); }
        void Fechar() => Escrever("\nendobj\n");

        Escrever("%PDF-1.7\n");
        ms.Write([(byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n']);

        Abrir(1);
        Escrever("<< /Type /Catalog /Pages 2 0 R" + (assinar ? " /AcroForm << /Fields [6 0 R] /SigFlags 3 >>" : "") + " >>");
        Fechar();

        Abrir(2);
        var kids = string.Join(" ", Enumerable.Range(0, paginas.Count).Select(i => $"{primeiraPagina + i * 2} 0 R"));
        Escrever($"<< /Type /Pages /Kids [{kids}] /Count {paginas.Count} >>");
        Fechar();

        Abrir(3);
        Escrever("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        Fechar();
        Abrir(4);
        Escrever("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        Fechar();

        var criacao = DataPdf(assinatura?.DataUtc ?? DateTime.UtcNow);
        Abrir(5);
        Escrever($"<< /Title {TextoPdf(conteudo.Titulo)} /Producer (Consultorio) /CreationDate ({criacao}) >>");
        Fechar();

        if (assinar)
        {
            Abrir(6);
            Escrever($"<< /Type /Annot /Subtype /Widget /FT /Sig /T (Assinatura1) /V 7 0 R /F 132 /Rect [0 0 0 0] /P {primeiraPagina} 0 R >>");
            Fechar();

            Abrir(7);
            Escrever("<< /Type /Sig /Filter /Adobe.PPKLite /SubFilter /ETSI.CAdES.detached /ByteRange ");
            posByteRange = (int)ms.Position;
            Escrever(new string(' ', TamanhoByteRange));
            Escrever(" /Contents ");
            inicioContents = (int)ms.Position;
            Escrever("<" + new string('0', TamanhoAssinaturaHex) + ">");
            fimContents = (int)ms.Position;
            Escrever($" /M ({criacao}) /Name {TextoPdf(assinatura!.Titular)} /Reason {TextoPdf(assinatura.Motivo)}");
            if (!string.IsNullOrWhiteSpace(assinatura.Local)) Escrever($" /Location {TextoPdf(assinatura.Local)}");
            Escrever(" >>");
            Fechar();
        }

        for (var i = 0; i < paginas.Count; i++)
        {
            var numeroPagina = primeiraPagina + i * 2;
            var conteudoPagina = Encoding.Latin1.GetBytes(Desenhar(conteudo, paginas[i], i + 1, paginas.Count));

            Abrir(numeroPagina);
            Escrever($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {LarguraPagina:0} {AlturaPagina:0}] " +
                     $"/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents {numeroPagina + 1} 0 R" +
                     (assinar && i == 0 ? " /Annots [6 0 R]" : "") + " >>");
            Fechar();

            Abrir(numeroPagina + 1);
            Escrever($"<< /Length {conteudoPagina.Length} >>\nstream\n");
            ms.Write(conteudoPagina);
            Escrever("\nendstream");
            Fechar();
        }

        var inicioXref = ms.Position;
        var xref = new StringBuilder();
        xref.Append($"xref\n0 {totalObjetos}\n0000000000 65535 f \n");
        for (var n = 1; n < totalObjetos; n++) xref.Append(offsets[n].ToString("D10", Invariante)).Append(" 00000 n \n");
        var id = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{conteudo.Titulo}|{criacao}|{Guid.NewGuid()}"))[..16]);
        xref.Append($"trailer\n<< /Size {totalObjetos} /Root 1 0 R /Info 5 0 R /ID [<{id}> <{id}>] >>\nstartxref\n{inicioXref}\n%%EOF\n");
        Escrever(xref.ToString());

        return new PdfMontado(ms.ToArray(), posByteRange, inicioContents, fimContents);
    }

    private static string Desenhar(ConteudoPdf conteudo, List<Linha> linhas, int pagina, int totalPaginas)
    {
        var sb = new StringBuilder();
        string N(float v) => v.ToString("0.##", Invariante);

        if (!string.IsNullOrWhiteSpace(conteudo.MarcaDagua))
        {
            // Marca d'água diagonal (ex.: CANCELADO), em cinza claro, atrás do texto.
            sb.Append($"q 0.85 g BT /F2 64 Tf 0.7071 0.7071 -0.7071 0.7071 {N(150)} {N(230)} Tm {TextoPdf(Sanitizar(conteudo.MarcaDagua))} Tj ET Q\n");
        }

        foreach (var linha in linhas)
        {
            if (linha.Regua)
            {
                sb.Append($"q 0.6 w 0 G {N(linha.X)} {N(linha.Y)} m {N(linha.X + linha.Tamanho)} {N(linha.Y)} l S Q\n");
                continue;
            }
            sb.Append($"BT {N(linha.Cinza)} g /{linha.Fonte} {N(linha.Tamanho)} Tf {N(linha.X)} {N(linha.Y)} Td {TextoPdf(linha.Texto)} Tj ET\n");
        }

        var rodape = string.IsNullOrWhiteSpace(conteudo.Rodape) ? string.Empty : Sanitizar(conteudo.Rodape).Replace('\n', ' ');
        var linhasRodape = Quebrar(rodape, false, 8, LarguraPagina - 2 * Margem - 60);
        var y = 52f;
        sb.Append($"q 0.4 w 0.75 G {N(Margem)} {N(y + 12)} m {N(LarguraPagina - Margem)} {N(y + 12)} l S Q\n");
        foreach (var linha in linhasRodape.Take(3))
        {
            sb.Append($"BT 0.4 g /F1 8 Tf {N(Margem)} {N(y)} Td {TextoPdf(linha)} Tj ET\n");
            y -= 10;
        }
        var textoPagina = $"Página {pagina} de {totalPaginas}";
        sb.Append($"BT 0.4 g /F1 8 Tf {N(LarguraPagina - Margem - Largura(textoPagina, false, 8))} 52 Td {TextoPdf(textoPagina)} Tj ET\n");
        return sb.ToString();
    }

    /// <summary>String literal do PDF, com escape de parênteses e barras.</summary>
    private static string TextoPdf(string? texto)
    {
        var limpo = Sanitizar(texto).Replace('\n', ' ');
        return "(" + limpo.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)") + ")";
    }

    private static string DataPdf(DateTime utc) => $"D:{utc.ToUniversalTime():yyyyMMddHHmmss}+00'00'";

    // ------------------------------------------------------------------ assinatura

    private static byte[] Assinar(PdfMontado pdf, X509Certificate2 certificado, X509Certificate2Collection cadeia)
    {
        var bytes = pdf.Bytes;
        var inicio = pdf.InicioContents;        // posição do "<"
        var fim = pdf.FimContents;              // logo após o ">"
        var byteRange = $"[0 {inicio} {fim} {bytes.Length - fim}]";
        if (byteRange.Length > TamanhoByteRange) throw new InvalidOperationException("ByteRange maior que o espaço reservado.");
        Encoding.Latin1.GetBytes(byteRange.PadRight(TamanhoByteRange)).CopyTo(bytes, pdf.PosicaoByteRange);

        // Conteúdo assinado: o arquivo inteiro, exceto o espaço do /Contents.
        var dados = new byte[inicio + (bytes.Length - fim)];
        Buffer.BlockCopy(bytes, 0, dados, 0, inicio);
        Buffer.BlockCopy(bytes, fim, dados, inicio, bytes.Length - fim);

        var cms = AssinarCms(dados, certificado, cadeia);
        var hex = Convert.ToHexString(cms);
        if (hex.Length > TamanhoAssinaturaHex) throw new InvalidOperationException("Assinatura maior que o espaço reservado no PDF.");
        Encoding.ASCII.GetBytes(hex).CopyTo(bytes, inicio + 1);
        return bytes;
    }

    /// <summary>CMS SignedData destacado (CAdES-BES): SHA-256 e atributo signing-certificate-v2.</summary>
    public static byte[] AssinarCms(byte[] dados, X509Certificate2 certificado, X509Certificate2Collection cadeia)
    {
        var cms = new SignedCms(new ContentInfo(dados), detached: true);
        var assinante = new CmsSigner(SubjectIdentifierType.IssuerAndSerialNumber, certificado)
        {
            DigestAlgorithm = new Oid("2.16.840.1.101.3.4.2.1"), // SHA-256
            IncludeOption = X509IncludeOption.EndCertOnly
        };
        foreach (var c in cadeia)
        {
            if (!string.Equals(c.Thumbprint, certificado.Thumbprint, StringComparison.OrdinalIgnoreCase)) assinante.Certificates.Add(c);
        }
        assinante.SignedAttributes.Add(new AsnEncodedData(new Oid("1.2.840.113549.1.9.16.2.47"), SigningCertificateV2(certificado)));
        cms.ComputeSignature(assinante, silent: true);
        return cms.Encode();
    }

    /// <summary>
    /// SigningCertificateV2 ::= SEQUENCE { certs SEQUENCE OF ESSCertIDv2 }
    /// ESSCertIDv2 ::= SEQUENCE { certHash OCTET STRING (SHA-256), issuerSerial IssuerSerial }
    /// </summary>
    private static byte[] SigningCertificateV2(X509Certificate2 certificado)
    {
        var escritor = new AsnWriter(AsnEncodingRules.DER);
        using (escritor.PushSequence())
        using (escritor.PushSequence())
        using (escritor.PushSequence())
        {
            escritor.WriteOctetString(SHA256.HashData(certificado.RawData));
            using (escritor.PushSequence()) // IssuerSerial
            {
                using (escritor.PushSequence()) // GeneralNames
                using (escritor.PushSequence(new Asn1Tag(TagClass.ContextSpecific, 4, isConstructed: true))) // directoryName [4]
                {
                    escritor.WriteEncodedValue(certificado.IssuerName.RawData);
                }
                escritor.WriteInteger(new BigInteger(certificado.GetSerialNumberBigEndian(), isUnsigned: true, isBigEndian: true));
            }
        }
        return escritor.Encode();
    }
}
