using System.ComponentModel.DataAnnotations;
using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Request;

public record ConvenioRequest(
    [Required(ErrorMessage = "Nome do convênio é obrigatório")]
    [StringLength(120, MinimumLength = 2, ErrorMessage = "O nome deve ter entre 2 e 120 caracteres")]
    string Nome,
    [StringLength(20)] string? RegistroAns = null,
    [StringLength(30)] string? Telefone = null,
    [StringLength(500)] string? Observacao = null
);

/// <summary>Convênio do paciente. IdConvenio nulo = atendimento particular.</summary>
public record ConvenioPacienteRequest(
    int? IdConvenio,
    [StringLength(30)] string? NumeroCarteirinha,
    DateTime? ValidadeCarteirinha
);

public record TabelaPrecoRequest(
    [Required] TipoAtendimento Tipo,
    int? IdConvenio,
    [Range(0, 100000, ErrorMessage = "Valor deve estar entre 0 e 100.000")] decimal Valor
);

public record CobrancaRequest(
    [Required] int IdPaciente,
    [Required(ErrorMessage = "Descrição é obrigatória")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "A descrição deve ter entre 2 e 200 caracteres")]
    string Descricao,
    [Range(0.01, 100000, ErrorMessage = "Valor deve estar entre 0,01 e 100.000")] decimal Valor,
    DateTime? DtVencimento = null,
    int? IdConvenio = null,
    int? IdAgenda = null
);

public record PagamentoRequest(
    [Required] FormaPagamento Forma,
    [Range(0, 100000)] decimal? Desconto = null,
    DateTime? DataPagamento = null
);
