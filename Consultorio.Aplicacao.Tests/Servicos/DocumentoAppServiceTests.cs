using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.Servicos;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Consultorio.Aplicacao.Tests.Servicos;

public class DocumentoAppServiceTests
{
    private const string EmailMedico = "roberto@clinica.com";

    private readonly IDocumentoClinicoRepositorio _documentoRepositorio = Substitute.For<IDocumentoClinicoRepositorio>();
    private readonly IPacienteRepositorio _pacienteRepositorio = Substitute.For<IPacienteRepositorio>();
    private readonly IMedicoRepositorio _medicoRepositorio = Substitute.For<IMedicoRepositorio>();
    private readonly IExameRepositorio _exameRepositorio = Substitute.For<IExameRepositorio>();
    private readonly OpcoesClinica _opcoes = new() { Nome = "Clínica Teste", UrlPublica = "https://clinica.exemplo" };
    private readonly DocumentoAppService _sut;

    private readonly Paciente _paciente;
    private readonly Medico _medico;

    public DocumentoAppServiceTests()
    {
        _sut = new DocumentoAppService(_documentoRepositorio, _pacienteRepositorio, _medicoRepositorio, _exameRepositorio, _opcoes);

        var pessoaPaciente = new Pessoa("Maria da Silva Souza", new Cpf("52998224725"), new DateTime(1980, 5, 10), new Telefone("62999990000"));
        _paciente = new Paciente(pessoaPaciente);
        DefinirId(_paciente, 1);

        var pessoaMedico = new Pessoa("Dr. Roberto Alves", new Cpf("11144477735"), new DateTime(1975, 3, 3), new Telefone("62988887777"));
        _medico = new Medico(pessoaMedico, new Crm("4321/GO"), [Especialidade.ClinicoGeral]);
        _medico.VincularUsuario(EmailMedico);
        DefinirId(_medico, 2);

        _pacienteRepositorio.ObterId(1).Returns(_paciente);
        _medicoRepositorio.ObterId(2).Returns(_medico);
        _documentoRepositorio.Adicionar(Arg.Any<DocumentoClinico>()).Returns(call =>
        {
            var doc = call.Arg<DocumentoClinico>();
            DefinirId(doc, 50);
            return doc;
        });
        _documentoRepositorio.Atualizar(Arg.Any<DocumentoClinico>()).Returns(call => call.Arg<DocumentoClinico>());
        _exameRepositorio.Adicionar(Arg.Any<Exame>()).Returns(call => call.Arg<Exame>());
    }

    private static void DefinirId(Entidade entidade, int id) =>
        typeof(Entidade).GetProperty(nameof(Entidade.Id))!.SetValue(entidade, id);

    private static void DefinirNavegacao<T>(DocumentoClinico doc, string propriedade, T valor) =>
        typeof(DocumentoClinico).GetProperty(propriedade)!.SetValue(doc, valor);

    [Fact]
    public async Task Emitir_PeloProprioMedico_DeveSalvarComCodigoEUrlDeVerificacao()
    {
        var request = new DocumentoRequest(TipoDocumentoClinico.Receita, 1, "Amoxicilina 500 mg — 1 cp de 8/8 h por 7 dias", IdMedico: 2);

        var resposta = await _sut.EmitirAsync(request, new UsuarioAtual(EmailMedico, EhAdministrador: false));

        resposta.Id.Should().Be(50);
        resposta.CodigoVerificacao.Should().HaveLength(10);
        resposta.UrlVerificacao.Should().Be($"https://clinica.exemplo/api/publico/documento/{resposta.CodigoVerificacao}");
        resposta.NomeClinica.Should().Be("Clínica Teste");
        await _documentoRepositorio.Received(1).Adicionar(Arg.Is<DocumentoClinico>(d => d.EmitidoPor == EmailMedico && d.IdMedico == 2));
    }

    [Fact]
    public async Task Emitir_MedicoLogadoEmNomeDeOutroMedico_DeveSerRecusado()
    {
        var outro = new Medico(new Pessoa("Dra. Ana Lima", new Cpf("52998224725"), new DateTime(1980, 1, 1), new Telefone("62977776666")),
            new Crm("9999/GO"), [Especialidade.Pediatra]);
        outro.VincularUsuario("ana@clinica.com");
        DefinirId(outro, 3);
        _medicoRepositorio.ObterPorEmailUsuario("ana@clinica.com").Returns(outro);

        var request = new DocumentoRequest(TipoDocumentoClinico.Receita, 1, "Dipirona 500 mg", IdMedico: 2);

        var act = () => _sut.EmitirAsync(request, new UsuarioAtual("ana@clinica.com", EhAdministrador: false));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*seu próprio nome*");
        await _documentoRepositorio.DidNotReceive().Adicionar(Arg.Any<DocumentoClinico>());
    }

    [Fact]
    public async Task Emitir_ParaPacienteInativo_DeveSerRecusado()
    {
        _paciente.Inativar();
        var request = new DocumentoRequest(TipoDocumentoClinico.Receita, 1, "Texto", IdMedico: 2);

        var act = () => _sut.EmitirAsync(request, new UsuarioAtual("admin@clinica.com", EhAdministrador: true));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inativo*");
    }

    [Fact]
    public async Task Emitir_PorMedicoInativo_DeveSerRecusado()
    {
        _medico.Inativar();
        var request = new DocumentoRequest(TipoDocumentoClinico.Receita, 1, "Texto", IdMedico: 2);

        var act = () => _sut.EmitirAsync(request, new UsuarioAtual("admin@clinica.com", EhAdministrador: true));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inativo*");
    }

    [Fact]
    public async Task Emitir_PedidoDeExames_DeveCriarUmExamePorLinha()
    {
        var request = new DocumentoRequest(TipoDocumentoClinico.PedidoExames, 1, "Hemograma completo\nGlicemia de jejum\n\nTSH", IdMedico: 2);

        var resposta = await _sut.EmitirAsync(request, new UsuarioAtual(EmailMedico, EhAdministrador: false));

        resposta.ExamesCriados.Should().Be(3);
        await _exameRepositorio.Received(3).Adicionar(Arg.Is<Exame>(e => e.IdPaciente == 1 && e.IdMedico == 2 && e.Status == StatusExame.Solicitado));
        await _exameRepositorio.Received(1).Adicionar(Arg.Is<Exame>(e => e.Descricao == "TSH"));
    }

    [Fact]
    public async Task Emitir_PedidoDeExamesSemCriarExames_NaoDeveTocarNaListaDeExames()
    {
        var request = new DocumentoRequest(TipoDocumentoClinico.PedidoExames, 1, "Hemograma", IdMedico: 2, CriarExames: false);

        var resposta = await _sut.EmitirAsync(request, new UsuarioAtual(EmailMedico, EhAdministrador: false));

        resposta.ExamesCriados.Should().Be(0);
        await _exameRepositorio.DidNotReceive().Adicionar(Arg.Any<Exame>());
    }

    [Fact]
    public async Task Emitir_AtestadoSemIncluirCid_NaoDeveImprimirODiagnostico()
    {
        var request = new DocumentoRequest(TipoDocumentoClinico.Atestado, 1, "Atesto...", IdMedico: 2, DiasAfastamento: 2,
            IncluirCid: false, CidCodigo: "J11", CidDescricao: "Influenza");

        var resposta = await _sut.EmitirAsync(request, new UsuarioAtual(EmailMedico, EhAdministrador: false));

        resposta.DiasAfastamento.Should().Be(2);
        resposta.CidCodigo.Should().BeNull();
    }

    [Fact]
    public async Task Emitir_AtestadoComCidAutorizado_DeveImprimirODiagnostico()
    {
        var request = new DocumentoRequest(TipoDocumentoClinico.Atestado, 1, "Atesto...", IdMedico: 2, DiasAfastamento: 2,
            IncluirCid: true, CidCodigo: "j11", CidDescricao: "Influenza");

        var resposta = await _sut.EmitirAsync(request, new UsuarioAtual(EmailMedico, EhAdministrador: false));

        resposta.CidCodigo.Should().Be("J11");
    }

    [Fact]
    public async Task Cancelar_PorOutroMedico_DeveSerRecusado()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", EmailMedico);
        DefinirNavegacao(doc, nameof(DocumentoClinico.Medico), _medico);
        _documentoRepositorio.ObterDetalhadoPorId(7).Returns(doc);

        var act = () => _sut.CancelarAsync(7, "Emitido por engano", new UsuarioAtual("ana@clinica.com", EhAdministrador: false));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*Somente o médico que emitiu*");
        doc.Cancelado.Should().BeFalse();
    }

    [Fact]
    public async Task Cancelar_PeloAdministrador_DeveCancelar()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", EmailMedico);
        DefinirNavegacao(doc, nameof(DocumentoClinico.Medico), _medico);
        _documentoRepositorio.ObterDetalhadoPorId(7).Returns(doc);

        var resposta = await _sut.CancelarAsync(7, "Emitido por engano", new UsuarioAtual("admin@clinica.com", EhAdministrador: true));

        resposta.Cancelado.Should().BeTrue();
        resposta.MotivoCancelamento.Should().Be("Emitido por engano");
    }

    [Fact]
    public async Task Verificar_DeveExporSoIniciaisDoPacienteENuncaOConteudo()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Atestado, 1, 2, "Conteúdo clínico sigiloso", EmailMedico, diasAfastamento: 3);
        DefinirNavegacao(doc, nameof(DocumentoClinico.Medico), _medico);
        DefinirNavegacao(doc, nameof(DocumentoClinico.Paciente), _paciente);
        _documentoRepositorio.ObterPorCodigo("ABCDEFGHJK").Returns(doc);

        var resposta = await _sut.VerificarAsync("ABCDEFGHJK");

        resposta.Encontrado.Should().BeTrue();
        resposta.IniciaisPaciente.Should().Be("M. D. S. S.");
        resposta.DiasAfastamento.Should().Be(3);
        resposta.CrmMedico.Should().Be(_medico.Crm.Valor);
        resposta.ToString().Should().NotContain("sigiloso");
    }

    [Fact]
    public async Task Verificar_CodigoInexistente_DeveIndicarNaoEncontrado()
    {
        var resposta = await _sut.VerificarAsync("NAOEXISTE");

        resposta.Encontrado.Should().BeFalse();
        resposta.NomeClinica.Should().Be("Clínica Teste");
    }
}
