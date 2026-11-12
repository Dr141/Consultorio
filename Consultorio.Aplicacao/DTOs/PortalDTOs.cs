using System.ComponentModel.DataAnnotations;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.DTOs.Portal;

public record PortalSolicitarCodigoRequest(
    [Required(ErrorMessage = "Informe o CPF")] [StringLength(14)] string Cpf,
    [Required(ErrorMessage = "Informe a data de nascimento")] DateTime DtNasc
);

public record PortalSolicitacaoResponse(Guid Chave, string Mensagem);

public record PortalValidarCodigoRequest(
    [Required] Guid Chave,
    [Required(ErrorMessage = "Informe o código")] [StringLength(10)] string Codigo
);

public record PortalSessaoResponse(string Token, DateTime ExpiraEm, string NomePaciente);

public record PortalAgendamentoResponse(
    int Id,
    DateTime DataHora,
    int DuracaoMinutos,
    string Tipo,
    string? NomeMedico,
    List<string> Especialidades,
    string? Status,
    bool PodeConfirmar,
    bool PodeCancelar
);

public record PortalResumoResponse(
    string Nome,
    string? Convenio,
    bool CarteirinhaVencida,
    List<PortalAgendamentoResponse> ProximosAgendamentos,
    List<PortalAgendamentoResponse> Historico,
    bool PodeAgendar,
    int MaximoAgendamentosFuturos,
    int AntecedenciaCancelamentoHoras,
    string NomeClinica,
    string? TelefoneClinica,
    string? EnderecoClinica
);

public record PortalHorarioResponse(int IdAgenda, DateTime DataHora, int DuracaoMinutos, string Tipo);

public record PortalMedicoHorariosResponse(int IdMedico, string Nome, List<string> Especialidades, List<PortalHorarioResponse> Horarios);

public record PortalExameResponse(
    int Id,
    string Descricao,
    DateTime DtSolicitacao,
    string Status,
    bool ResultadoDisponivel,
    string? Resultado,
    DateTime? DtLaudo,
    string? NomeMedico,
    List<ExameAnexoResponse> Anexos
);
