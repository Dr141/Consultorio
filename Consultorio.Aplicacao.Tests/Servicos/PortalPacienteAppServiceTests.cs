using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Portal;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
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

public class PortalPacienteAppServiceTests
{
    private sealed class RelogioFixo(DateTimeOffset agora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => agora;
    }

    // 10/03/2027 15:00 em Brasília = 18:00 UTC
    private static readonly DateTimeOffset Agora = new(2027, 3, 10, 18, 0, 0, TimeSpan.Zero);
    private static readonly DateTime HojeLocal = new(2027, 3, 10);
    private const string CpfPaciente = "52998224725";

    private readonly IPessoaRepositorio _pessoas = Substitute.For<IPessoaRepositorio>();
    private readonly IPacienteRepositorio _pacientes = Substitute.For<IPacienteRepositorio>();
    private readonly IAcessoPortalRepositorio _acessos = Substitute.For<IAcessoPortalRepositorio>();
    private readonly IAgendaRepositorio _agendas = Substitute.For<IAgendaRepositorio>();
    private readonly IAgendaAppService _agendaApp = Substitute.For<IAgendaAppService>();
    private readonly IBloqueioAgendaRepositorio _bloqueios = Substitute.For<IBloqueioAgendaRepositorio>();
    private readonly IExameRepositorio _exames = Substitute.For<IExameRepositorio>();
    private readonly IExameAnexoRepositorio _anexos = Substitute.For<IExameAnexoRepositorio>();
    private readonly IDocumentoClinicoRepositorio _documentos = Substitute.For<IDocumentoClinicoRepositorio>();
    private readonly ICobrancaRepositorio _cobrancas = Substitute.For<ICobrancaRepositorio>();
    private readonly INotificador _notificador = Substitute.For<INotificador>();
    private readonly OpcoesClinica _opcoes = new()
    {
        FusoHorario = "Inexistente/Fixo", Nome = "Clínica Teste",
        PortalMaximoAgendamentosFuturos = 2, PortalAntecedenciaCancelamentoHoras = 12, PortalAntecedenciaAgendamentoHoras = 2
    };

    private readonly Pessoa _pessoa;
    private readonly Paciente _paciente;

    public PortalPacienteAppServiceTests()
    {
        _pessoa = new Pessoa("Joana Prado", new Cpf(CpfPaciente), new DateTime(1990, 5, 20), new Telefone("62999990000"));
        _pessoa.DefinirEmail("joana@email.com");
        DefinirId(_pessoa, 3);
        _paciente = new Paciente(_pessoa);
        DefinirId(_paciente, 7);

        _pessoas.ObterPorCpf(CpfPaciente).Returns(_pessoa);
        _pacientes.ObterPorIdPessoa(3).Returns(_paciente);
        _pacientes.ObterId(7).Returns(_paciente);
        _pacientes.ObterDetalhadoPorId(7).Returns(_paciente);
        _acessos.Adicionar(Arg.Any<AcessoPortal>()).Returns(c => c.Arg<AcessoPortal>());
        _acessos.Atualizar(Arg.Any<AcessoPortal>()).Returns(c => c.Arg<AcessoPortal>());
        _notificador.PodeEnviar(Arg.Any<MensagemNotificacao>()).Returns(true);
        _notificador.EnviarAsync(Arg.Any<MensagemNotificacao>(), Arg.Any<CancellationToken>()).Returns(true);
        _agendas.ObterPorPaciente(Arg.Any<int>()).Returns(new List<Agenda>());
        _bloqueios.ObterNoPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>()).Returns(new List<BloqueioAgenda>());
    }

    private PortalPacienteAppService CriarSut() =>
        new(_pessoas, _pacientes, _acessos, _agendas, _agendaApp, _bloqueios, _exames, _anexos, _documentos, _cobrancas,
            [_notificador], _opcoes, new RelogioFixo(Agora));

    private static void DefinirId(Entidade e, int id) => typeof(Entidade).GetProperty(nameof(Entidade.Id))!.SetValue(e, id);

    private static Medico MedicoAtivo()
    {
        var medico = new Medico(new Pessoa("Dr. Roberto Alves", new Cpf("11144477735"), new DateTime(1975, 3, 3), new Telefone("62988887777")),
            new Crm("4321/GO"), [Especialidade.ClinicoGeral]);
        DefinirId(medico, 2);
        return medico;
    }

    private static Agenda Horario(int id, DateTime dia, int hora, int? idPaciente = null, TipoAtendimento tipo = TipoAtendimento.Consulta)
    {
        var agenda = new Agenda(dia, new TimeSpan(hora, 0, 0), idMedico: 2, idPaciente: idPaciente, tipo: tipo);
        DefinirId(agenda, id);
        typeof(Agenda).GetProperty(nameof(Agenda.Medico))!.SetValue(agenda, MedicoAtivo());
        return agenda;
    }

    // ---------- Acesso ----------

    [Fact]
    public async Task SolicitarCodigo_DadosCorretos_DeveEnviarCodigoPorEmail()
    {
        var resposta = await CriarSut().SolicitarCodigoAsync(new PortalSolicitarCodigoRequest(CpfPaciente, new DateTime(1990, 5, 20)), "10.0.0.1");

        resposta.Mensagem.Should().Be(PortalPacienteAppService.MensagemSolicitacao);
        await _acessos.Received(1).Adicionar(Arg.Is<AcessoPortal>(a => a.IdPaciente == 7 && a.Chave == resposta.Chave));
        await _notificador.Received(1).EnviarAsync(Arg.Is<MensagemNotificacao>(m => m.Email == "joana@email.com"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SolicitarCodigo_DataDeNascimentoErrada_DeveResponderIgualSemEnviar()
    {
        var resposta = await CriarSut().SolicitarCodigoAsync(new PortalSolicitarCodigoRequest(CpfPaciente, new DateTime(1991, 1, 1)), null);

        resposta.Mensagem.Should().Be(PortalPacienteAppService.MensagemSolicitacao);
        resposta.Chave.Should().NotBeEmpty();
        await _acessos.DidNotReceive().Adicionar(Arg.Any<AcessoPortal>());
        await _notificador.DidNotReceiveWithAnyArgs().EnviarAsync(default!, default);
    }

    [Fact]
    public async Task SolicitarCodigo_LimitePorHora_NaoDeveGerarNovoCodigo()
    {
        _acessos.ContarSolicitacoes(7, Arg.Any<DateTime>()).Returns(5);

        await CriarSut().SolicitarCodigoAsync(new PortalSolicitarCodigoRequest(CpfPaciente, new DateTime(1990, 5, 20)), null);

        await _acessos.DidNotReceive().Adicionar(Arg.Any<AcessoPortal>());
    }

    [Fact]
    public async Task ValidarCodigo_Errado_DeveRecusarESomarTentativa()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora.UtcDateTime, TimeSpan.FromMinutes(10));
        _acessos.ObterPorChave(acesso.Chave).Returns(acesso);
        var errado = codigo == "000000" ? "111111" : "000000";

        var act = () => CriarSut().ValidarCodigoAsync(new PortalValidarCodigoRequest(acesso.Chave, errado));

        await act.Should().ThrowAsync<DomainException>().WithMessage("*inválido*");
        acesso.Tentativas.Should().Be(1);
    }

    [Fact]
    public async Task ValidarCodigo_Certo_DeveDevolverTokenQueValidaASessao()
    {
        var (acesso, codigo) = AcessoPortal.Solicitar(7, Agora.UtcDateTime, TimeSpan.FromMinutes(10));
        _acessos.ObterPorChave(acesso.Chave).Returns(acesso);
        var sut = CriarSut();

        var sessao = await sut.ValidarCodigoAsync(new PortalValidarCodigoRequest(acesso.Chave, codigo));
        _acessos.ObterPorTokenHash(AcessoPortal.HashToken(sessao.Token)).Returns(acesso);

        sessao.NomePaciente.Should().Be("Joana");
        (await sut.ValidarSessaoAsync(sessao.Token)).Should().Be(7);
        (await sut.ValidarSessaoAsync("token-qualquer")).Should().BeNull();
    }

    // ---------- Agenda ----------

    [Fact]
    public async Task Agendar_HorarioLivreDentroDasRegras_DeveAgendar()
    {
        var livre = Horario(50, HojeLocal.AddDays(3), 9);
        _agendas.ObterDetalhadoPorId(50).Returns(livre);

        await CriarSut().AgendarAsync(7, 50);

        await _agendaApp.Received(1).AgendarPacienteAsync(50, 7);
    }

    [Fact]
    public async Task Agendar_ComMenosDeDuasHorasDeAntecedencia_DeveRecusar()
    {
        var emCimaDaHora = Horario(51, HojeLocal, 16); // agora são 15:00 locais
        _agendas.ObterDetalhadoPorId(51).Returns(emCimaDaHora);

        var act = () => CriarSut().AgendarAsync(7, 51);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*não está mais disponível*");
        await _agendaApp.DidNotReceiveWithAnyArgs().AgendarPacienteAsync(default, default);
    }

    [Fact]
    public async Task Agendar_HorarioDeProcedimento_NaoEOferecidoNoPortal()
    {
        _agendas.ObterDetalhadoPorId(52).Returns(Horario(52, HojeLocal.AddDays(3), 9, tipo: TipoAtendimento.Procedimento));

        var act = () => CriarSut().AgendarAsync(7, 52);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Agendar_AcimaDoLimiteDeAgendamentosFuturos_DeveRecusar()
    {
        _agendas.ObterDetalhadoPorId(53).Returns(Horario(53, HojeLocal.AddDays(5), 9));
        _agendas.ObterPorPaciente(7).Returns(new List<Agenda>
        {
            Horario(60, HojeLocal.AddDays(1), 9, idPaciente: 7),
            Horario(61, HojeLocal.AddDays(2), 9, idPaciente: 7)
        });

        var act = () => CriarSut().AgendarAsync(7, 53);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*já tem 2 agendamento*");
    }

    [Fact]
    public async Task Cancelar_AgendamentoDeOutroPaciente_DeveParecerInexistente()
    {
        _agendas.ObterDetalhadoPorId(70).Returns(Horario(70, HojeLocal.AddDays(3), 9, idPaciente: 99));

        var act = () => CriarSut().CancelarAsync(7, 70);

        await act.Should().ThrowAsync<DomainException>().WithMessage("Agendamento não encontrado.");
        await _agendaApp.DidNotReceiveWithAnyArgs().LiberarHorarioAsync(default);
    }

    [Fact]
    public async Task Cancelar_ComMenosDe12Horas_DeveOrientarLigarParaAClinica()
    {
        _agendas.ObterDetalhadoPorId(71).Returns(Horario(71, HojeLocal.AddDays(1), 1, idPaciente: 7)); // amanhã 01:00 = 10 h

        var act = () => CriarSut().CancelarAsync(7, 71);

        await act.Should().ThrowAsync<DomainException>().WithMessage("*12 horas*");
    }

    [Fact]
    public async Task Cancelar_ComAntecedencia_DeveLiberarOHorario()
    {
        _agendas.ObterDetalhadoPorId(72).Returns(Horario(72, HojeLocal.AddDays(2), 9, idPaciente: 7));

        await CriarSut().CancelarAsync(7, 72);

        await _agendaApp.Received(1).LiberarHorarioAsync(72);
    }

    [Fact]
    public async Task Horarios_DeveOferecerSoLivresDeConsultaOuRetornoNoFuturo()
    {
        _agendas.ObterPorPeriodo(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int?>()).Returns(new List<Agenda>
        {
            Horario(80, HojeLocal.AddDays(1), 9),
            Horario(81, HojeLocal.AddDays(1), 10, idPaciente: 99),
            Horario(82, HojeLocal.AddDays(1), 11, tipo: TipoAtendimento.Exame),
            Horario(83, HojeLocal, 8),
            Horario(84, HojeLocal.AddDays(2), 14, tipo: TipoAtendimento.Retorno)
        });

        var medicos = await CriarSut().ObterHorariosDisponiveisAsync(7);

        medicos.Should().ContainSingle();
        medicos[0].Horarios.Select(h => h.IdAgenda).Should().Equal(80, 84);
        medicos[0].Especialidades.Should().Contain("Clínico Geral");
    }

    // ---------- Exames e documentos ----------

    [Fact]
    public async Task Exames_SoMostraOResultadoDepoisDoLaudo()
    {
        var pendente = new Exame(7, "Hemograma");
        var laudado = new Exame(7, "Glicemia", "92 mg/dL");
        DefinirId(pendente, 1);
        DefinirId(laudado, 2);
        _exames.ObterPorPaciente(7).Returns(new List<Exame> { pendente, laudado });
        _anexos.ObterResumoPorExame(Arg.Any<int>()).Returns(new List<ExameAnexo>());

        var exames = await CriarSut().ObterExamesAsync(7);

        exames.Single(e => e.Id == 1).Resultado.Should().BeNull();
        exames.Single(e => e.Id == 2).Resultado.Should().Be("92 mg/dL");
    }

    [Fact]
    public async Task Exames_ResultadosNaoLiberados_NaoDevemAparecer()
    {
        _opcoes.PortalLiberarResultados = false;
        var laudado = new Exame(7, "Glicemia", "92 mg/dL");
        _exames.ObterPorPaciente(7).Returns(new List<Exame> { laudado });

        var exames = await CriarSut().ObterExamesAsync(7);

        exames.Single().Resultado.Should().BeNull();
        exames.Single().ResultadoDisponivel.Should().BeFalse();
    }

    [Fact]
    public async Task Documento_DeOutroPaciente_NaoDeveSerDevolvido()
    {
        var doc = new DocumentoClinico(TipoDocumentoClinico.Receita, 99, 2, "Texto", "medico@clinica.com");
        _documentos.ObterDetalhadoPorId(5).Returns(doc);

        (await CriarSut().ObterDocumentoAsync(7, 5)).Should().BeNull();
    }
}
