using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IConvenioAppService
{
    Task<List<ConvenioResponse>> ObterTodosAsync(bool incluirInativos = false);
    Task<ConvenioResponse?> ObterPorIdAsync(int id);
    Task<ConvenioResponse> AdicionarAsync(ConvenioRequest request);
    Task<ConvenioResponse> AtualizarAsync(int id, ConvenioRequest request);
    Task<ConvenioResponse> InativarAsync(int id);
    Task<ConvenioResponse> ReativarAsync(int id);

    Task<List<TabelaPrecoResponse>> ObterPrecosAsync();
    /// <summary>Cria ou atualiza o valor do tipo de atendimento (particular ou do convênio).</summary>
    Task<TabelaPrecoResponse> DefinirPrecoAsync(TabelaPrecoRequest request);
    Task RemoverPrecoAsync(int id);
}

public interface IFinanceiroAppService
{
    Task<List<CobrancaResponse>> ObterAsync(DateTime? inicio, DateTime? fim, string? status = null, int? idPaciente = null);
    Task<List<CobrancaResponse>> ObterPendentesAsync();
    Task<CobrancaResponse?> ObterPorIdAsync(int id);
    Task<CobrancaResponse> CriarAsync(CobrancaRequest request, UsuarioAtual usuario);
    /// <summary>Gera a cobrança de um atendimento concluído usando a tabela de preços.</summary>
    Task<CobrancaResponse> GerarDoAtendimentoAsync(int idAgenda, UsuarioAtual usuario);
    Task<CobrancaResponse> RegistrarPagamentoAsync(int id, PagamentoRequest request, UsuarioAtual usuario);
    Task<CobrancaResponse> CancelarAsync(int id, string motivo);
    Task<CobrancaResponse> EstornarAsync(int id, string motivo);
    Task<ReciboResponse> ObterReciboAsync(int id);
    Task<ResumoFinanceiroResponse> ObterResumoAsync(DateTime inicio, DateTime fim);
    /// <summary>Rotina: cria as cobranças dos atendimentos concluídos no período que ainda não têm cobrança.</summary>
    Task<int> GerarCobrancasDeAtendimentosAsync(DateTime inicio, DateTime fim, CancellationToken cancellationToken = default);
}

public interface IIndicadoresAppService
{
    Task<IndicadoresResponse> ObterAsync(DateTime inicio, DateTime fim, int? idMedico = null);
    Task<PendenciasResponse> ObterPendenciasAsync();
}

public interface IRotinasClinicaAppService
{
    Task<int> GerarCobrancasAsync(CancellationToken cancellationToken = default);
    Task<ResultadoRotinaResponse> AvisarLaudosProntosAsync(CancellationToken cancellationToken = default);
    Task<ResultadoRotinaResponse> LembrarRetornosAsync(CancellationToken cancellationToken = default);
    /// <summary>Uma vez por dia, a partir da hora configurada: agenda do dia para cada médico e pendências para a gestão.</summary>
    Task<bool> EnviarResumoDiarioAsync(CancellationToken cancellationToken = default);
    /// <summary>No início de cada mês: indicadores do mês anterior para a gestão.</summary>
    Task<bool> EnviarIndicadoresMensaisAsync(CancellationToken cancellationToken = default);
}

/// <summary>E-mails da equipe por perfil (implementado no servidor, que conhece os usuários do Identity).</summary>
public interface IDestinatariosInternos
{
    Task<List<string>> ObterEmailsAsync(params string[] perfis);
}
