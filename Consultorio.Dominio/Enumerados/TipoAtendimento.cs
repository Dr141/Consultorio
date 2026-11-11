namespace Consultorio.Dominio.Enumerados;

public enum TipoAtendimento
{
    Consulta = 1,
    Retorno = 2,
    Exame = 3,
    Procedimento = 4
}

public static class TipoAtendimentoExtensoes
{
    public static string ObterNome(this TipoAtendimento tipo) => tipo switch
    {
        TipoAtendimento.Consulta => "Consulta",
        TipoAtendimento.Retorno => "Retorno",
        TipoAtendimento.Exame => "Exame",
        TipoAtendimento.Procedimento => "Procedimento",
        _ => tipo.ToString()
    };
}
