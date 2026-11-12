using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.ValueObjects;

/// <summary>
/// Sinais vitais aferidos no atendimento. Todos opcionais; os limites recusam valores
/// fisiologicamente impossíveis (geralmente erro de digitação), não valores clínicos anormais.
/// </summary>
public class SinaisVitais
{
    public int? PressaoSistolica { get; private set; }
    public int? PressaoDiastolica { get; private set; }
    public int? FrequenciaCardiaca { get; private set; }
    public int? FrequenciaRespiratoria { get; private set; }
    public decimal? Temperatura { get; private set; }
    public int? SaturacaoO2 { get; private set; }
    public decimal? PesoKg { get; private set; }
    public int? AlturaCm { get; private set; }
    public int? Glicemia { get; private set; }

    protected SinaisVitais() { }

    public SinaisVitais(
        int? pressaoSistolica = null, int? pressaoDiastolica = null,
        int? frequenciaCardiaca = null, int? frequenciaRespiratoria = null,
        decimal? temperatura = null, int? saturacaoO2 = null,
        decimal? pesoKg = null, int? alturaCm = null, int? glicemia = null)
    {
        Faixa(pressaoSistolica, 40, 300, "Pressão sistólica");
        Faixa(pressaoDiastolica, 20, 200, "Pressão diastólica");
        DomainException.When(pressaoSistolica.HasValue != pressaoDiastolica.HasValue,
            "Informe a pressão arterial completa (sistólica e diastólica).");
        DomainException.When(pressaoSistolica.HasValue && pressaoDiastolica >= pressaoSistolica,
            "A pressão diastólica deve ser menor que a sistólica.");
        Faixa(frequenciaCardiaca, 20, 300, "Frequência cardíaca");
        Faixa(frequenciaRespiratoria, 4, 80, "Frequência respiratória");
        Faixa(temperatura, 30m, 45m, "Temperatura");
        Faixa(saturacaoO2, 40, 100, "Saturação de O₂");
        Faixa(pesoKg, 0.3m, 400m, "Peso");
        Faixa(alturaCm, 25, 250, "Altura");
        Faixa(glicemia, 10, 1500, "Glicemia");

        PressaoSistolica = pressaoSistolica;
        PressaoDiastolica = pressaoDiastolica;
        FrequenciaCardiaca = frequenciaCardiaca;
        FrequenciaRespiratoria = frequenciaRespiratoria;
        Temperatura = temperatura;
        SaturacaoO2 = saturacaoO2;
        PesoKg = pesoKg;
        AlturaCm = alturaCm;
        Glicemia = glicemia;
    }

    /// <summary>Índice de massa corporal (kg/m²), quando peso e altura foram informados.</summary>
    public decimal? Imc => PesoKg.HasValue && AlturaCm is > 0
        ? Math.Round(PesoKg.Value / ((AlturaCm.Value / 100m) * (AlturaCm.Value / 100m)), 1)
        : null;

    public bool Vazio =>
        PressaoSistolica is null && PressaoDiastolica is null && FrequenciaCardiaca is null &&
        FrequenciaRespiratoria is null && Temperatura is null && SaturacaoO2 is null &&
        PesoKg is null && AlturaCm is null && Glicemia is null;

    private static void Faixa(int? valor, int minimo, int maximo, string nome) =>
        DomainException.When(valor.HasValue && (valor < minimo || valor > maximo), $"{nome} fora do intervalo aceitável ({minimo}–{maximo}).");

    private static void Faixa(decimal? valor, decimal minimo, decimal maximo, string nome) =>
        DomainException.When(valor.HasValue && (valor < minimo || valor > maximo), $"{nome} fora do intervalo aceitável ({minimo}–{maximo}).");
}
