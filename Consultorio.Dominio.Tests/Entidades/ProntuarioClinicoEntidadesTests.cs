using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Consultorio.Dominio.Tests.Entidades;

public class ConsultaEvolucaoTests
{
    [Fact]
    public void Criar_ComEvolucaoSoap_DevePreencherCampos()
    {
        var consulta = new Consulta(1, 2, new EvolucaoClinica(null, "Tosse", "Ausculta limpa", "IVAS", "Sintomáticos"));

        consulta.Observacao.Should().BeEmpty();
        consulta.Subjetivo.Should().Be("Tosse");
        consulta.Plano.Should().Be("Sintomáticos");
    }

    [Fact]
    public void RegistrarSinaisVitais_Vazio_DeveGuardarNulo()
    {
        var consulta = new Consulta(1, 2, "Evolução");

        consulta.RegistrarSinaisVitais(new SinaisVitais());

        consulta.SinaisVitais.Should().BeNull();
    }

    [Fact]
    public void DefinirDiagnostico_DeveGuardarERemoverCid()
    {
        var consulta = new Consulta(1, 2, "Evolução");

        consulta.DefinirDiagnostico(new Cid("J069", "Infecção aguda das vias aéreas superiores"));
        consulta.CidCodigo.Should().Be("J06.9");

        consulta.DefinirDiagnostico(null);
        consulta.CidCodigo.Should().BeNull();
        consulta.CidDescricao.Should().BeNull();
    }

    [Fact]
    public void AlterarEvolucao_AposFinalizar_DeveSerRecusado()
    {
        var consulta = new Consulta(1, 2, "Evolução");
        consulta.Finalizar();

        var evolucao = () => consulta.AtualizarEvolucao(new EvolucaoClinica("Nova"));
        var sinais = () => consulta.RegistrarSinaisVitais(new SinaisVitais(frequenciaCardiaca: 80));
        var cid = () => consulta.DefinirDiagnostico(new Cid("I10", null));

        evolucao.Should().Throw<DomainException>().WithMessage("*adendo*");
        sinais.Should().Throw<DomainException>();
        cid.Should().Throw<DomainException>();
    }
}

public class PacienteAlertasTests
{
    private static Paciente CriarPaciente() =>
        new(new Pessoa("Maria Souza", new Cpf("52998224725"), new DateTime(1980, 1, 1), new Telefone("62999990000")));

    [Fact]
    public void AtualizarAlertas_DeveNormalizarUmItemPorLinhaSemRepetir()
    {
        var paciente = CriarPaciente();

        paciente.AtualizarAlertas(" Dipirona \n\npenicilina\nDIPIRONA\n", null, "  ");

        paciente.Alergias.Should().Be("Dipirona\npenicilina");
        paciente.MedicamentosEmUso.Should().BeNull();
        paciente.CondicoesCronicas.Should().BeNull();
    }

    [Fact]
    public void AtualizarAlertas_ComTextoLongoDemais_DeveLancarDomainException()
    {
        var paciente = CriarPaciente();

        var act = () => paciente.AtualizarAlertas(new string('a', 2001), null, null);

        act.Should().Throw<DomainException>().WithMessage("Alergias*");
    }
}

public class DocumentoClinicoTests
{
    [Fact]
    public void Criar_Receita_DeveGerarCodigoDeVerificacao()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Amoxicilina 500 mg", "medico@clinica.com");

        doc.CodigoVerificacao.Should().HaveLength(10).And.MatchRegex("^[A-HJ-NP-Z2-9]{10}$");
        doc.Cancelado.Should().BeFalse();
        doc.DiasAfastamento.Should().BeNull();
    }

    [Fact]
    public void Criar_DoisDocumentos_DevemTerCodigosDiferentes()
    {
        var a = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", "medico@clinica.com");
        var b = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", "medico@clinica.com");

        a.CodigoVerificacao.Should().NotBe(b.CodigoVerificacao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(366)]
    public void Criar_AtestadoSemDiasValidos_DeveLancarDomainException(int? dias)
    {
        var act = () => new DocumentoClinico(TipoDocumentoClinico.Atestado, 1, 2, "Atesto...", "medico@clinica.com", diasAfastamento: dias);

        act.Should().Throw<DomainException>().WithMessage("*dias de afastamento*");
    }

    [Fact]
    public void Criar_ReceitaComDiasDeAfastamento_DeveLancarDomainException()
    {
        var act = () => new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", "medico@clinica.com", diasAfastamento: 2);

        act.Should().Throw<DomainException>().WithMessage("*só se aplicam a atestados*");
    }

    [Fact]
    public void Criar_AtestadoComCid_DeveGuardarCid()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Atestado, 1, 2, "Atesto...", "medico@clinica.com",
            diasAfastamento: 3, cid: new Cid("J11", "Influenza"));

        doc.DiasAfastamento.Should().Be(3);
        doc.CidCodigo.Should().Be("J11");
        doc.CidDescricao.Should().Be("Influenza");
    }

    [Fact]
    public void ItensDoPedido_DeveIgnorarLinhasVaziasEMarcadores()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.PedidoExames, 1, 2, "- Hemograma completo\n\n• Glicemia de jejum\r\n  TSH  ", "medico@clinica.com");

        doc.ItensDoPedido().Should().Equal("Hemograma completo", "Glicemia de jejum", "TSH");
    }

    [Fact]
    public void Cancelar_DeveRegistrarMotivoENaoPermitirCancelarDuasVezes()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", "medico@clinica.com");

        doc.Cancelar("Emitido para o paciente errado");

        doc.Cancelado.Should().BeTrue();
        doc.MotivoCancelamento.Should().Be("Emitido para o paciente errado");
        doc.DtCancelamento.Should().NotBeNull();
        var act = () => doc.Cancelar("Outra tentativa");
        act.Should().Throw<DomainException>().WithMessage("*já está cancelado*");
    }

    [Fact]
    public void Cancelar_SemMotivoSuficiente_DeveLancarDomainException()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 1, 2, "Texto", "medico@clinica.com");

        var act = () => doc.Cancelar("erro");

        act.Should().Throw<DomainException>().WithMessage("*motivo*");
    }
}

public class ExameCicloTests
{
    [Fact]
    public void CicloCompleto_DeveAvancarSolicitadoRealizadoLaudadoEntregue()
    {
        var exame = new Exame(1, "Hemograma completo");
        exame.Status.Should().Be(StatusExame.Solicitado);

        exame.RegistrarRealizacao();
        exame.Status.Should().Be(StatusExame.Realizado);
        exame.DtRealizacao.Should().NotBeNull();

        exame.RegistrarResultado("Sem alterações");
        exame.Status.Should().Be(StatusExame.Laudado);
        exame.DtLaudo.Should().NotBeNull();

        exame.RegistrarEntrega();
        exame.Status.Should().Be(StatusExame.Entregue);
        exame.DtEntrega.Should().NotBeNull();
    }

    [Fact]
    public void RegistrarResultado_SemRealizacaoPrevia_DevePreencherDataDeRealizacao()
    {
        var exame = new Exame(1, "Glicemia");

        exame.RegistrarResultado("98 mg/dL");

        exame.Status.Should().Be(StatusExame.Laudado);
        exame.DtRealizacao.Should().NotBeNull();
    }

    [Fact]
    public void Criar_ComResultado_DeveNascerLaudado()
    {
        new Exame(1, "TSH", "2,1 mUI/L").Status.Should().Be(StatusExame.Laudado);
    }

    [Fact]
    public void RegistrarEntrega_SemLaudo_DeveLancarDomainException()
    {
        var exame = new Exame(1, "TSH");

        var act = () => exame.RegistrarEntrega();

        act.Should().Throw<DomainException>().WithMessage("*laudo*");
    }

    [Fact]
    public void RegistrarRealizacao_DuasVezes_DeveLancarDomainException()
    {
        var exame = new Exame(1, "TSH");
        exame.RegistrarRealizacao();

        var act = () => exame.RegistrarRealizacao();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RegistrarRealizacao_ExameCancelado_DeveLancarDomainException()
    {
        var exame = new Exame(1, "TSH");
        exame.Cancelar("Paciente desistiu do exame");

        var act = () => exame.RegistrarRealizacao();

        act.Should().Throw<DomainException>().WithMessage("*cancelado*");
    }
}

public class ExameAnexoTests
{
    private static readonly byte[] Pdf = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void Criar_Pdf_DeveDetectarTipoPeloConteudo()
    {
        var anexo = new ExameAnexo(1, "resultado.pdf", Pdf, "recepcao@clinica.com");

        anexo.ContentType.Should().Be("application/pdf");
        anexo.TamanhoBytes.Should().Be(Pdf.Length);
        anexo.NomeArquivo.Should().Be("resultado.pdf");
    }

    [Fact]
    public void Criar_ImagensPngEJpeg_DevemSerAceitas()
    {
        new ExameAnexo(1, "raio-x.png", Png, "x").ContentType.Should().Be("image/png");
        new ExameAnexo(1, "foto.jpg", Jpeg, "x").ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public void Criar_ComExtensaoEnganosa_DeveConsiderarOConteudo()
    {
        // Um executável renomeado para .pdf não passa pela verificação de assinatura do arquivo.
        var act = () => new ExameAnexo(1, "laudo.pdf", [0x4D, 0x5A, 0x90, 0x00], "x");

        act.Should().Throw<DomainException>().WithMessage("*Formato não suportado*");
    }

    [Fact]
    public void Criar_ArquivoVazio_DeveLancarDomainException()
    {
        var act = () => new ExameAnexo(1, "vazio.pdf", [], "x");

        act.Should().Throw<DomainException>().WithMessage("*vazio*");
    }

    [Fact]
    public void Criar_NomeComCaminho_DeveManterSoONomeDoArquivo()
    {
        var anexo = new ExameAnexo(1, "../../etc/resultado.pdf", Pdf, "x");

        anexo.NomeArquivo.Should().Be("resultado.pdf");
    }
}
