namespace Consultorio.Aplicacao.DTOs.Response;

public record ConvenioResponse(
    int Id,
    string Nome,
    string? RegistroAns,
    string? Telefone,
    string? Observacao,
    bool Ativo,
    DateTime DtCriacao
);

public record TabelaPrecoResponse(
    int Id,
    string Tipo,
    string TipoNome,
    int? IdConvenio,
    string? NomeConvenio,
    decimal Valor,
    DateTime? DtAtualizacao
);

public record CobrancaResponse(
    int Id,
    int IdPaciente,
    string NomePaciente,
    string? CpfPaciente,
    int? IdAgenda,
    int? IdConvenio,
    string? NomeConvenio,
    string Descricao,
    decimal Valor,
    decimal Desconto,
    decimal ValorFinal,
    string Status,
    DateTime DtVencimento,
    bool Vencida,
    string? FormaPagamento,
    string? FormaPagamentoNome,
    DateTime? DtPagamento,
    string? RecebidoPor,
    DateTime? DtCancelamento,
    string? MotivoCancelamento,
    string RegistradoPor,
    DateTime DtCriacao
);

/// <summary>Agrupamento genérico usado em resumos e indicadores.</summary>
public record ItemIndicador(string Rotulo, int Quantidade, decimal Valor = 0);

public record ResumoFinanceiroResponse(
    decimal Recebido,
    int QuantidadeRecebida,
    decimal TicketMedio,
    decimal AReceber,
    decimal Vencido,
    decimal Estornado,
    List<ItemIndicador> PorForma,
    List<ItemIndicador> PorConvenio
);

public record ReciboResponse(
    CobrancaResponse Cobranca,
    string ValorPorExtenso,
    string NomeClinica,
    string? EnderecoClinica,
    string? TelefoneClinica,
    DateTime DtEmissao
);

public record IndicadorMedico(int IdMedico, string Nome, int Agendados, int Atendidos, int Faltas);

public record IndicadoresResponse(
    DateTime Inicio,
    DateTime Fim,
    int Agendamentos,
    int Atendidos,
    int Faltas,
    decimal TaxaFaltas,
    decimal TaxaConfirmacao,
    int HorariosLivres,
    decimal TaxaOcupacao,
    int NovosPacientes,
    int ConsultasRegistradas,
    int ExamesSolicitados,
    int ExamesLaudados,
    int ExamesAtrasados,
    decimal? TempoMedioLaudoDias,
    ResumoFinanceiroResponse Financeiro,
    List<IndicadorMedico> PorMedico,
    List<ItemIndicador> PorTipoAtendimento,
    List<ItemIndicador> DiagnosticosFrequentes
);

public record PendenciaItem(
    string Categoria,
    string Titulo,
    string Detalhe,
    DateTime? Data,
    int? IdPaciente,
    int? IdReferencia
);

public record PendenciasResponse(
    List<PendenciaItem> ExamesAtrasados,
    List<PendenciaItem> LaudosNaoEntregues,
    List<PendenciaItem> Carteirinhas,
    List<PendenciaItem> CobrancasVencidas,
    List<PendenciaItem> ConsultasAbertas,
    List<PendenciaItem> RetornosSemAgendamento
)
{
    public int Total => ExamesAtrasados.Count + LaudosNaoEntregues.Count + Carteirinhas.Count
                        + CobrancasVencidas.Count + ConsultasAbertas.Count + RetornosSemAgendamento.Count;
}

public record AcessoUsuarioResponse(
    string Email,
    List<string> Perfis,
    bool EmailConfirmado,
    bool Bloqueado,
    DateTime? UltimaAtividade,
    int? DiasSemAtividade,
    bool RevisarAcesso,
    string? MotivoRevisao
);
