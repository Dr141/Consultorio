using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Servicos;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class CertificadoDigitalTests
{
    private static readonly DateTime Agora = new(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc);

    private static DadosCertificado Dados(DateTime? de = null, DateTime? ate = null, bool chave = true) =>
        new("DR TESTE", "AC TESTE", "01", "ABC", de ?? Agora.AddYears(-1), ate ?? Agora.AddYears(1), chave, IcpBrasil: true);

    [Fact]
    public void Criar_ComDadosValidos_DeveFicarVigente()
    {
        var certificado = new CertificadoDigital(3, Dados(), [1, 2], [3], "medico@clinica.com", Agora);

        certificado.Ativo.Should().BeTrue();
        certificado.VigenteEm(Agora).Should().BeTrue();
        certificado.VigenteEm(Agora.AddYears(2)).Should().BeFalse();
    }

    [Fact]
    public void Criar_Vencido_DeveSerRecusado()
    {
        var criar = () => new CertificadoDigital(3, Dados(ate: Agora.AddDays(-1)), [1], [1], "x", Agora);
        criar.Should().Throw<DomainException>().WithMessage("Certificado vencido*");
    }

    [Fact]
    public void Criar_SemChavePrivada_DeveSerRecusado()
    {
        var criar = () => new CertificadoDigital(3, Dados(chave: false), [1], [1], "x", Agora);
        criar.Should().Throw<DomainException>().WithMessage("*chave privada*");
    }

    [Fact]
    public void Desativar_DeveApagarArquivoESenha()
    {
        var certificado = new CertificadoDigital(3, Dados(), [1, 2], [3], "x", Agora);

        certificado.Desativar(Agora);

        certificado.Ativo.Should().BeFalse();
        certificado.PfxProtegido.Should().BeEmpty();
        certificado.SenhaProtegida.Should().BeEmpty();
        certificado.VigenteEm(Agora).Should().BeFalse();
        certificado.Invoking(c => c.Desativar(Agora)).Should().Throw<DomainException>();
    }
}

public class DocumentoClinicoAssinaturaTests
{
    private static DocumentoClinico Documento() => new(TipoDocumentoClinico.Receita, 1, 2, "Dipirona 500 mg", "medico@clinica.com");

    [Fact]
    public void RegistrarAssinatura_DeveGuardarTitularEData()
    {
        var documento = Documento();
        var quando = new DateTime(2026, 11, 12, 15, 0, 0, DateTimeKind.Utc);

        documento.RegistrarAssinatura("DR TESTE", quando);

        documento.Assinado.Should().BeTrue();
        documento.AssinadoPor.Should().Be("DR TESTE");
        documento.DtAssinatura.Should().Be(quando);
    }

    [Fact]
    public void RegistrarAssinatura_DuasVezes_DeveSerRecusado()
    {
        var documento = Documento();
        documento.RegistrarAssinatura("DR TESTE", DateTime.UtcNow);
        documento.Invoking(d => d.RegistrarAssinatura("DR TESTE", DateTime.UtcNow)).Should().Throw<DomainException>();
    }

    [Fact]
    public void RegistrarAssinatura_DocumentoCancelado_DeveSerRecusado()
    {
        var documento = Documento();
        documento.Cancelar("Emitido por engano");
        documento.Invoking(d => d.RegistrarAssinatura("DR TESTE", DateTime.UtcNow)).Should().Throw<DomainException>();
    }

    [Fact]
    public void DocumentoAssinado_AindaPodeSerCancelado()
    {
        var documento = Documento();
        documento.RegistrarAssinatura("DR TESTE", DateTime.UtcNow);
        documento.Cancelar("Dose errada na receita");
        documento.Cancelado.Should().BeTrue();
    }
}
