using System.Net;
using System.Text.Json;
using Consultorio.Dominio.Interfaces.Servicos;
using Consultorio.Infraestrutura.Notificacoes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Consultorio.Server.Tests.Notificacoes;

public class WhatsAppNotificadorTests
{
    /// <summary>Handler falso: guarda a requisição e devolve a resposta configurada.</summary>
    private sealed class HandlerFalso(HttpStatusCode status, string corpo = "{}") : HttpMessageHandler
    {
        public HttpRequestMessage? Requisicao { get; private set; }
        public string? CorpoEnviado { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requisicao = request;
            CorpoEnviado = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status) { Content = new StringContent(corpo) };
        }
    }

    private static readonly OpcoesWhatsApp Configurado = new()
    {
        Habilitado = true, Token = "token-teste", PhoneNumberId = "123456", VersaoApi = "v21.0", Template = "aviso_clinica"
    };

    private static MensagemNotificacao Mensagem(string? telefone, string texto = "Olá, Maria!\n\nSeu exame está pronto.\nAcesse o portal.") =>
        new("Maria da Silva", "maria@exemplo.com", telefone, "Assunto", texto, null);

    private static (WhatsAppNotificador Sut, HandlerFalso Handler) Criar(OpcoesWhatsApp opcoes, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new HandlerFalso(status, status == HttpStatusCode.OK ? "{\"messages\":[{\"id\":\"wamid.1\"}]}" : "{\"error\":{\"code\":132001}}");
        return (new WhatsAppNotificador(new HttpClient(handler), opcoes, NullLogger<WhatsAppNotificador>.Instance), handler);
    }

    [Theory]
    [InlineData("(62) 98765-4321", "5562987654321")]
    [InlineData("62987654321", "5562987654321")]
    [InlineData("+55 62 98765-4321", "5562987654321")]
    [InlineData("062987654321", "5562987654321")]
    public void NumeroWhatsApp_CelularBrasileiro_DeveFormatarE164(string telefone, string esperado) =>
        WhatsAppNotificador.NumeroWhatsApp(telefone).Should().Be(esperado);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("(62) 3333-0000")]   // fixo
    [InlineData("6233330000")]
    [InlineData("12345")]
    public void NumeroWhatsApp_SemCelular_DeveRetornarNulo(string? telefone) =>
        WhatsAppNotificador.NumeroWhatsApp(telefone).Should().BeNull();

    [Fact]
    public void TextoParametro_DeveRemoverSaudacaoEQuebrasDeLinha()
    {
        WhatsAppNotificador.TextoParametro("Olá, Maria!\n\nSeu exame está pronto.\n\tAcesse   o portal.")
            .Should().Be("Seu exame está pronto. Acesse o portal.");
    }

    [Fact]
    public void TextoParametro_DeveLimitarTamanho()
    {
        var texto = WhatsAppNotificador.TextoParametro(new string('a', 2000), 100);
        texto.Length.Should().Be(100);
        texto.Should().EndWith("…");
    }

    [Fact]
    public void PodeEnviar_DependeDeConfiguracaoECelular()
    {
        Criar(Configurado).Sut.PodeEnviar(Mensagem("62987654321")).Should().BeTrue();
        Criar(Configurado).Sut.PodeEnviar(Mensagem("6233330000")).Should().BeFalse();
        Criar(new OpcoesWhatsApp { Habilitado = false, Token = "t", PhoneNumberId = "1" }).Sut
            .PodeEnviar(Mensagem("62987654321")).Should().BeFalse();
        Criar(new OpcoesWhatsApp { Habilitado = true, Token = null, PhoneNumberId = "1" }).Sut
            .PodeEnviar(Mensagem("62987654321")).Should().BeFalse();
    }

    [Fact]
    public async Task EnviarAsync_DeveChamarCloudApiComModelo()
    {
        var (sut, handler) = Criar(Configurado);

        var enviado = await sut.EnviarAsync(Mensagem("(62) 98765-4321"));

        enviado.Should().BeTrue();
        handler.Requisicao!.Method.Should().Be(HttpMethod.Post);
        handler.Requisicao.RequestUri!.ToString().Should().Be("https://graph.facebook.com/v21.0/123456/messages");
        handler.Requisicao.Headers.Authorization!.Scheme.Should().Be("Bearer");
        handler.Requisicao.Headers.Authorization.Parameter.Should().Be("token-teste");

        using var json = JsonDocument.Parse(handler.CorpoEnviado!);
        var raiz = json.RootElement;
        raiz.GetProperty("messaging_product").GetString().Should().Be("whatsapp");
        raiz.GetProperty("to").GetString().Should().Be("5562987654321");
        raiz.GetProperty("type").GetString().Should().Be("template");
        var template = raiz.GetProperty("template");
        template.GetProperty("name").GetString().Should().Be("aviso_clinica");
        template.GetProperty("language").GetProperty("code").GetString().Should().Be("pt_BR");
        var parametros = template.GetProperty("components")[0].GetProperty("parameters");
        parametros[0].GetProperty("text").GetString().Should().Be("Maria");
        parametros[1].GetProperty("text").GetString().Should().Be("Seu exame está pronto. Acesse o portal.");
    }

    [Fact]
    public async Task EnviarAsync_ErroDaApi_DeveRetornarFalse()
    {
        var (sut, _) = Criar(Configurado, HttpStatusCode.BadRequest);
        (await sut.EnviarAsync(Mensagem("62987654321"))).Should().BeFalse();
    }

    [Fact]
    public async Task EnviarAsync_SemCelular_NaoDeveChamarApi()
    {
        var (sut, handler) = Criar(Configurado);
        (await sut.EnviarAsync(Mensagem("6233330000"))).Should().BeFalse();
        handler.Requisicao.Should().BeNull();
    }
}
