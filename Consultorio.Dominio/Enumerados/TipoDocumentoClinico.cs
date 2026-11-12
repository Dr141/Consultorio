namespace Consultorio.Dominio.Enumerados;

public enum TipoDocumentoClinico
{
    Receita = 1,
    ReceitaControleEspecial = 2,
    Atestado = 3,
    DeclaracaoComparecimento = 4,
    PedidoExames = 5,
    Encaminhamento = 6,
    Relatorio = 7
}

public static class TipoDocumentoClinicoExtensoes
{
    public static string ObterNome(this TipoDocumentoClinico tipo) => tipo switch
    {
        TipoDocumentoClinico.Receita => "Receita",
        TipoDocumentoClinico.ReceitaControleEspecial => "Receita de controle especial",
        TipoDocumentoClinico.Atestado => "Atestado médico",
        TipoDocumentoClinico.DeclaracaoComparecimento => "Declaração de comparecimento",
        TipoDocumentoClinico.PedidoExames => "Pedido de exames",
        TipoDocumentoClinico.Encaminhamento => "Encaminhamento",
        TipoDocumentoClinico.Relatorio => "Relatório médico",
        _ => tipo.ToString()
    };
}
