using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Infraestrutura.Pdf;
using FluentAssertions;
using Xunit;

namespace Consultorio.Server.Tests.Pdf;

public class GeradorPdfTests
{
    private const string Senha = "senha-teste";
    private readonly GeradorPdf _gerador = new();

    /// <summary>Certificado autoassinado no formato de um e-CPF ("NOME:CPF"), exportado como .pfx.</summary>
    internal static byte[] CriarPfx(string organizacao = "ICP-Brasil", DateTimeOffset? inicio = null, DateTimeOffset? fim = null)
    {
        using var rsa = RSA.Create(2048);
        var requisicao = new CertificateRequest($"CN=DR TESTE DA SILVA:12345678900, O={organizacao}, C=BR", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        requisicao.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));
        var agora = DateTimeOffset.UtcNow;
        using var certificado = requisicao.CreateSelfSigned(inicio ?? agora.AddDays(-1), fim ?? agora.AddYears(1));
        return certificado.Export(X509ContentType.Pfx, Senha);
    }

    private static ConteudoPdf Conteudo(int linhasCorpo = 5, string? marca = null)
    {
        var corpo = string.Join("\n", Enumerable.Range(1, linhasCorpo).Select(i => $"{i}. Amoxicilina 500 mg — tomar 1 cápsula de 8/8 h (ação “contínua”)"));
        return new ConteudoPdf("Receita - Maria", [
            new BlocoPdf("Clínica São José", EstiloBlocoPdf.Cabecalho),
            new BlocoPdf("Rua das Acácias, 100 (centro) \\ Goiânia", EstiloBlocoPdf.Pequeno),
            new BlocoPdf(string.Empty, EstiloBlocoPdf.Linha, 8),
            new BlocoPdf("Receita", EstiloBlocoPdf.Titulo, 16),
            new BlocoPdf(corpo, EstiloBlocoPdf.Normal, 14),
            new BlocoPdf("220", EstiloBlocoPdf.Linha, 40),
            new BlocoPdf("Dr. Teste", EstiloBlocoPdf.CentralizadoNegrito)
        ], "Código de verificação ABCDEFGHJK", marca);
    }

    private static (int[] ByteRange, byte[] Cms) ExtrairAssinatura(byte[] pdf)
    {
        var texto = Encoding.Latin1.GetString(pdf);
        var inicio = texto.IndexOf("/ByteRange [", StringComparison.Ordinal);
        inicio.Should().BeGreaterThan(0);
        var fim = texto.IndexOf(']', inicio);
        var faixa = texto[(inicio + "/ByteRange [".Length)..fim]
            .Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

        var hex = texto[(faixa[1] + 1)..(faixa[2] - 1)];
        var bytes = Convert.FromHexString(hex);
        // O espaço reservado é completado com zeros: lê só o valor DER.
        AsnDecoder.ReadEncodedValue(bytes, AsnEncodingRules.BER, out _, out _, out var tamanho);
        return (faixa, bytes[..tamanho]);
    }

    private static byte[] BytesAssinados(byte[] pdf, int[] faixa) =>
        pdf[..faixa[1]].Concat(pdf[faixa[2]..]).ToArray();

    [Fact]
    public void Gerar_SemAssinatura_DeveProduzirPdfValido()
    {
        var pdf = _gerador.Gerar(Conteudo());
        var texto = Encoding.Latin1.GetString(pdf);

        texto.Should().StartWith("%PDF-1.7");
        texto.TrimEnd().Should().EndWith("%%EOF");
        texto.Should().Contain("/BaseFont /Helvetica");
        texto.Should().Contain("(Clínica São José)").And.NotContain("/ByteRange");
        texto.Should().Contain("(RECEITA)", "o título sai em maiúsculas");
        texto.Should().Contain("\\(centro\\) \\\\ Goiânia", "parênteses e barras são escapados");
        texto.Should().Contain("/Count 1");
    }

    [Fact]
    public void Gerar_TextoLongo_DeveQuebrarEmVariasPaginas()
    {
        var pdf = _gerador.Gerar(Conteudo(linhasCorpo: 150));
        var texto = Encoding.Latin1.GetString(pdf);
        texto.Should().MatchRegex(@"/Count [2-9]");
        texto.Should().Contain("(Página 2 de ");
    }

    [Fact]
    public void Gerar_XrefDeveApontarParaOsObjetos()
    {
        var pdf = _gerador.Gerar(Conteudo(), new CertificadoAssinatura(CriarPfx(), Senha, "Teste", "Clínica"));
        var texto = Encoding.Latin1.GetString(pdf);
        var inicioXref = int.Parse(texto[(texto.LastIndexOf("startxref", StringComparison.Ordinal) + 10)..].Split('\n')[0]);
        texto[inicioXref..].Should().StartWith("xref");

        var entradas = texto[inicioXref..].Split('\n').Skip(3).TakeWhile(l => l.EndsWith(" n ", StringComparison.Ordinal)).ToList();
        entradas.Should().NotBeEmpty();
        for (var i = 0; i < entradas.Count; i++)
        {
            var offset = int.Parse(entradas[i][..10]);
            texto[offset..].Should().StartWith($"{i + 1} 0 obj");
        }
    }

    [Fact]
    public void Gerar_ComCertificado_DeveConterAssinaturaPadesValida()
    {
        var pdf = _gerador.Gerar(Conteudo(), new CertificadoAssinatura(CriarPfx(), Senha, "Receita emitida", "Clínica"));
        var texto = Encoding.Latin1.GetString(pdf);
        texto.Should().Contain("/SubFilter /ETSI.CAdES.detached").And.Contain("/SigFlags 3").And.Contain("/FT /Sig");

        var (faixa, der) = ExtrairAssinatura(pdf);
        faixa[0].Should().Be(0);
        (faixa[2] + faixa[3]).Should().Be(pdf.Length, "a assinatura cobre o arquivo inteiro, exceto o próprio valor");

        var cms = new SignedCms(new ContentInfo(BytesAssinados(pdf, faixa)), detached: true);
        cms.Decode(der);
        var verificar = () => cms.CheckSignature(verifySignatureOnly: true);
        verificar.Should().NotThrow();

        var assinante = cms.SignerInfos[0];
        assinante.DigestAlgorithm.Value.Should().Be("2.16.840.1.101.3.4.2.1");
        assinante.Certificate!.Subject.Should().Contain("DR TESTE DA SILVA");
        assinante.SignedAttributes.Cast<CryptographicAttributeObject>()
            .Should().Contain(a => a.Oid.Value == "1.2.840.113549.1.9.16.2.47", "CAdES exige o atributo signing-certificate-v2");
    }

    [Fact]
    public void Gerar_PdfAlteradoDepoisDeAssinado_DeveInvalidarAssinatura()
    {
        var pdf = _gerador.Gerar(Conteudo(), new CertificadoAssinatura(CriarPfx(), Senha, "Receita emitida", null));
        var (faixa, der) = ExtrairAssinatura(pdf);

        var adulterado = (byte[])pdf.Clone();
        var posicao = Encoding.Latin1.GetString(adulterado).IndexOf("Amoxicilina", StringComparison.Ordinal);
        adulterado[posicao] = (byte)'B';

        var cms = new SignedCms(new ContentInfo(BytesAssinados(adulterado, faixa)), detached: true);
        cms.Decode(der);
        var verificar = () => cms.CheckSignature(verifySignatureOnly: true);
        verificar.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Gerar_SenhaErrada_DeveLancarDomainException()
    {
        var gerar = () => _gerador.Gerar(Conteudo(), new CertificadoAssinatura(CriarPfx(), "errada", "x", null));
        gerar.Should().Throw<DomainException>().WithMessage("*senha*");
    }

    [Fact]
    public void Sanitizar_DeveTrocarCaracteresForaDoWinAnsi()
    {
        GeradorPdf.Sanitizar("SpO₂ — “ok” • fim…\r\nnova").Should().Be("SpO2 - \"ok\" - fim...\nnova");
        GeradorPdf.Sanitizar("ação ÇÃO º").Should().Be("ação ÇÃO º");
        GeradorPdf.Sanitizar("emoji 😀").Should().Be("emoji ??");
    }

    [Fact]
    public void Gerar_ComMarcaDagua_DeveIncluirTexto()
    {
        var texto = Encoding.Latin1.GetString(_gerador.Gerar(Conteudo(marca: "CANCELADO")));
        texto.Should().Contain("(CANCELADO) Tj");
    }
}

public class LeitorCertificadoTests
{
    private readonly LeitorCertificado _leitor = new();

    [Fact]
    public void Ler_DeveExtrairTitularSemCpfEIdentificarIcpBrasil()
    {
        var dados = _leitor.Ler(GeradorPdfTests.CriarPfx(), "senha-teste");

        dados.Titular.Should().Be("DR TESTE DA SILVA");
        dados.PossuiChavePrivada.Should().BeTrue();
        dados.IcpBrasil.Should().BeTrue();
        dados.Thumbprint.Should().HaveLength(64);
        dados.ValidoAteUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Ler_CertificadoDeOutraAutoridade_NaoEhIcpBrasil()
    {
        _leitor.Ler(GeradorPdfTests.CriarPfx("Empresa Qualquer"), "senha-teste").IcpBrasil.Should().BeFalse();
    }

    [Fact]
    public void Ler_SenhaErrada_DeveLancarDomainException()
    {
        var ler = () => _leitor.Ler(GeradorPdfTests.CriarPfx(), "outra");
        ler.Should().Throw<DomainException>().WithMessage("*senha*");
    }

    [Fact]
    public void Ler_ArquivoInvalido_DeveLancarDomainException()
    {
        var ler = () => _leitor.Ler(Encoding.ASCII.GetBytes("isto não é um pfx"), "x");
        ler.Should().Throw<DomainException>();
    }
}
