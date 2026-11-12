using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class AssinaturaDigitalAppServiceTests
{
    private const string EmailMedico = "roberto@clinica.com";
    private static readonly DateTime Agora = new(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc);

    private sealed class RelogioFixo(DateTime agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(agora);
    }

    /// <summary>"Criptografia" reversível só para teste: inverte os bytes.</summary>
    private sealed class ProtetorInvertido : IProtetorDados
    {
        public byte[] Proteger(byte[] dados) => dados.Reverse().ToArray();
        public byte[] Desproteger(byte[] dadosProtegidos) => dadosProtegidos.Reverse().ToArray();
    }

    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly ICertificadoDigitalRepositorio _certificadoRepositorio = Substitute.For<ICertificadoDigitalRepositorio>();
    private readonly IDocumentoClinicoRepositorio _documentoRepositorio = Substitute.For<IDocumentoClinicoRepositorio>();
    private readonly IDocumentoAssinaturaRepositorio _assinaturaRepositorio = Substitute.For<IDocumentoAssinaturaRepositorio>();
    private readonly ILeitorCertificado _leitor = Substitute.For<ILeitorCertificado>();
    private readonly IGeradorPdf _gerador = Substitute.For<IGeradorPdf>();
    private readonly OpcoesClinica _opcoes = new() { Nome = "Clínica Teste", UrlPublica = "https://clinica.exemplo", FusoHorario = "Inexistente/Fixo" };
    private readonly AssinaturaDigitalAppService _sut;
    private readonly Medico _medico;
    private readonly DocumentoClinico _documento;

    private static readonly DadosCertificado DadosValidos = new("DR ROBERTO ALVES", "AC TESTE", "01AB", new string('A', 64),
        Agora.AddYears(-1), Agora.AddYears(1), PossuiChavePrivada: true, IcpBrasil: true);

    public AssinaturaDigitalAppServiceTests()
    {
        _sut = new AssinaturaDigitalAppService(_medicoRepositorio, _certificadoRepositorio, _documentoRepositorio, _assinaturaRepositorio,
            _leitor, _gerador, new ProtetorInvertido(), _opcoes, new RelogioFixo(Agora));

        _medico = new Medico(new Pessoa("Dr. Roberto Alves", new Cpf("11144477735"), new DateTime(1975, 3, 3), new Telefone("62988887777")),
            new Crm("4321/GO"), [Especialidade.ClinicoGeral]);
        _medico.VincularUsuario(EmailMedico);
        DefinirId(_medico, 2);
        _medicoRepositorio.ObterId(2).Returns(_medico);

        var paciente = new Paciente(new Pessoa("Maria da Silva", new Cpf("52998224725"), new DateTime(1980, 5, 10), new Telefone("62999990000")));
        DefinirId(paciente, 1);
        _documento = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Dipirona 500 mg", EmailMedico);
        DefinirId(_documento, 50);
        typeof(DocumentoClinico).GetProperty(nameof(DocumentoClinico.Paciente))!.SetValue(_documento, paciente);
        typeof(DocumentoClinico).GetProperty(nameof(DocumentoClinico.Medico))!.SetValue(_documento, _medico);
        _documentoRepositorio.ObterDetalhadoPorId(50).Returns(_documento);
        _documentoRepositorio.Atualizar(Arg.Any<DocumentoClinico>()).Returns(c => c.Arg<DocumentoClinico>());

        _leitor.Ler(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(DadosValidos);
        _certificadoRepositorio.Adicionar(Arg.Any<CertificadoDigital>()).Returns(c => c.Arg<CertificadoDigital>());
        _certificadoRepositorio.Atualizar(Arg.Any<CertificadoDigital>()).Returns(c => c.Arg<CertificadoDigital>());
        _assinaturaRepositorio.Adicionar(Arg.Any<DocumentoAssinatura>()).Returns(c => c.Arg<DocumentoAssinatura>());
        _gerador.Gerar(Arg.Any<ConteudoPdf>(), Arg.Any<CertificadoAssinatura?>()).Returns(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 });
    }

    private static void DefinirId(Entidade entidade, int id) =>
        typeof(Entidade).GetProperty(nameof(Entidade.Id))!.SetValue(entidade, id);

    private static UsuarioAtual UsuarioMedico => new(EmailMedico, EhAdministrador: false);

    private CertificadoDigital CertificadoAtivo()
    {
        var protetor = new ProtetorInvertido();
        var certificado = new CertificadoDigital(2, DadosValidos, protetor.Proteger([1, 2, 3]), protetor.Proteger("senha"u8.ToArray()), EmailMedico, Agora);
        _certificadoRepositorio.ObterAtivoPorMedico(2).Returns(certificado);
        return certificado;
    }

    [Fact]
    public async Task EnviarCertificado_PeloProprioMedico_DeveGuardarProtegidoESubstituirOAnterior()
    {
        var anterior = CertificadoAtivo();

        var resposta = await _sut.EnviarCertificadoAsync(2, [9, 8, 7], "senha", UsuarioMedico);

        resposta.Titular.Should().Be("DR ROBERTO ALVES");
        resposta.IcpBrasil.Should().BeTrue();
        resposta.DiasParaVencer.Should().BeGreaterThan(300);
        anterior.Ativo.Should().BeFalse();
        anterior.PfxProtegido.Should().BeEmpty("o arquivo do certificado substituído é apagado");
        await _certificadoRepositorio.Received(1).Adicionar(Arg.Is<CertificadoDigital>(c =>
            c.PfxProtegido.SequenceEqual(new byte[] { 7, 8, 9 }) && c.Ativo));
    }

    [Fact]
    public async Task EnviarCertificado_DeOutroMedicoSemSerAdministrador_DeveSerRecusado()
    {
        var enviar = () => _sut.EnviarCertificadoAsync(2, [1], "senha", new UsuarioAtual("outro@clinica.com", false));
        await enviar.Should().ThrowAsync<DomainException>().WithMessage("*próprio médico*");
        await _certificadoRepositorio.DidNotReceive().Adicionar(Arg.Any<CertificadoDigital>());
    }

    [Fact]
    public async Task EnviarCertificado_SemChavePrivada_DeveSerRecusado()
    {
        _leitor.Ler(Arg.Any<byte[]>(), Arg.Any<string>()).Returns(DadosValidos with { PossuiChavePrivada = false });
        var enviar = () => _sut.EnviarCertificadoAsync(2, [1], "senha", UsuarioMedico);
        await enviar.Should().ThrowAsync<DomainException>().WithMessage("*chave privada*");
    }

    [Fact]
    public async Task Assinar_DeveGerarPdfAssinadoERegistrarNoDocumento()
    {
        CertificadoAtivo();

        var resposta = await _sut.AssinarDocumentoAsync(50, UsuarioMedico);

        resposta.Assinado.Should().BeTrue();
        resposta.AssinadoPor.Should().Be("DR ROBERTO ALVES");
        _documento.DtAssinatura.Should().Be(Agora);
        _gerador.Received(1).Gerar(
            Arg.Is<ConteudoPdf>(c => c.Blocos.Any(b => b.Texto.Contains("assinado digitalmente por DR ROBERTO ALVES"))),
            Arg.Is<CertificadoAssinatura?>(a => a != null && a.Senha == "senha" && a.Pfx.Length == 3));
        await _assinaturaRepositorio.Received(1).Adicionar(Arg.Is<DocumentoAssinatura>(a => a.IdDocumento == 50 && a.AssinadoPor == EmailMedico));
    }

    [Fact]
    public async Task Assinar_PorOutroUsuario_DeveSerRecusado()
    {
        CertificadoAtivo();
        var assinar = () => _sut.AssinarDocumentoAsync(50, new UsuarioAtual("admin@clinica.com", EhAdministrador: true));
        await assinar.Should().ThrowAsync<DomainException>().WithMessage("*médico que emitiu*");
    }

    [Fact]
    public async Task Assinar_SemCertificado_DeveOrientarCadastro()
    {
        var assinar = () => _sut.AssinarDocumentoAsync(50, UsuarioMedico);
        await assinar.Should().ThrowAsync<DomainException>().WithMessage("*Cadastre o certificado*");
    }

    [Fact]
    public async Task Assinar_DocumentoCancelado_DeveSerRecusado()
    {
        CertificadoAtivo();
        _documento.Cancelar("Emitido por engano");
        var assinar = () => _sut.AssinarDocumentoAsync(50, UsuarioMedico);
        await assinar.Should().ThrowAsync<DomainException>().WithMessage("*cancelado*");
    }

    [Fact]
    public async Task ObterPdf_DocumentoAssinado_DeveDevolverOArquivoGuardado()
    {
        _documento.RegistrarAssinatura("DR ROBERTO ALVES", Agora);
        byte[] guardado = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        _assinaturaRepositorio.ObterPorDocumento(50).Returns(new DocumentoAssinatura(50, guardado, "DR ROBERTO ALVES", "AA", EmailMedico));

        var arquivo = await _sut.ObterPdfAsync(50);

        arquivo!.Conteudo.Should().BeSameAs(guardado);
        arquivo.NomeArquivo.Should().EndWith("-assinado.pdf");
        arquivo.ContentType.Should().Be("application/pdf");
        _gerador.DidNotReceive().Gerar(Arg.Any<ConteudoPdf>(), Arg.Any<CertificadoAssinatura?>());
    }

    [Fact]
    public async Task ObterPdf_DocumentoCancelado_DeveGerarComMarcaDagua()
    {
        _documento.Cancelar("Emitido por engano");

        var arquivo = await _sut.ObterPdfAsync(50);

        arquivo!.NomeArquivo.Should().Be($"receita-{_documento.CodigoVerificacao}.pdf");
        _gerador.Received(1).Gerar(Arg.Is<ConteudoPdf>(c => c.MarcaDagua == "CANCELADO"), Arg.Is<CertificadoAssinatura?>(a => a == null));
    }

    [Fact]
    public async Task ObterPdfPortal_DocumentoDeOutroPaciente_DeveRetornarNulo()
    {
        (await _sut.ObterPdfPortalAsync(idPaciente: 99, idDocumento: 50)).Should().BeNull();
    }
}
