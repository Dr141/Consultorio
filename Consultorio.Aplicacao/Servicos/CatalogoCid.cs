using System.Globalization;
using System.Text;
using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>
/// Lista resumida de códigos CID-10 frequentes na atenção primária, para busca rápida no atendimento.
/// Não substitui a tabela oficial (DATASUS): qualquer código válido pode ser digitado manualmente.
/// Para usar a tabela completa, carregue-a nesta classe (ou em uma tabela do banco) a partir do arquivo oficial.
/// </summary>
public static class CatalogoCid
{
    private static readonly CidResponse[] Itens =
    [
        new("A09", "Diarreia e gastroenterite de origem infecciosa presumível"),
        new("A90", "Dengue (dengue clássico)"),
        new("B01.9", "Varicela sem complicação"),
        new("B34.9", "Infecção viral não especificada"),
        new("B35.3", "Tinha dos pés"),
        new("B37.3", "Candidíase da vulva e da vagina"),
        new("B86", "Escabiose"),
        new("E03.9", "Hipotireoidismo não especificado"),
        new("E05.9", "Tireotoxicose não especificada"),
        new("E10.9", "Diabetes mellitus insulinodependente, sem complicações"),
        new("E11.9", "Diabetes mellitus não insulinodependente, sem complicações"),
        new("E55.9", "Deficiência não especificada de vitamina D"),
        new("E66.9", "Obesidade não especificada"),
        new("E78.0", "Hipercolesterolemia pura"),
        new("E78.5", "Hiperlipidemia não especificada"),
        new("F17.2", "Transtornos devidos ao uso de fumo – síndrome de dependência"),
        new("F32.9", "Episódio depressivo não especificado"),
        new("F41.1", "Ansiedade generalizada"),
        new("F41.2", "Transtorno misto ansioso e depressivo"),
        new("F41.9", "Transtorno ansioso não especificado"),
        new("F51.0", "Insônia não orgânica"),
        new("G43.9", "Enxaqueca, sem especificação"),
        new("G44.2", "Cefaleia tensional"),
        new("G47.0", "Distúrbios do início e da manutenção do sono (insônias)"),
        new("H10.9", "Conjuntivite não especificada"),
        new("H52.4", "Presbiopia"),
        new("H60.9", "Otite externa não especificada"),
        new("H66.9", "Otite média não especificada"),
        new("I10", "Hipertensão essencial (primária)"),
        new("I20.9", "Angina pectoris, não especificada"),
        new("I25.9", "Doença isquêmica crônica do coração não especificada"),
        new("I48", "Flutter e fibrilação atrial"),
        new("I50.9", "Insuficiência cardíaca não especificada"),
        new("I83.9", "Varizes dos membros inferiores sem úlcera ou inflamação"),
        new("I84.9", "Hemorroidas sem complicação, não especificadas"),
        new("J00", "Nasofaringite aguda (resfriado comum)"),
        new("J01.9", "Sinusite aguda não especificada"),
        new("J02.9", "Faringite aguda não especificada"),
        new("J03.9", "Amigdalite aguda não especificada"),
        new("J06.9", "Infecção aguda das vias aéreas superiores não especificada"),
        new("J11", "Influenza (gripe) devida a vírus não identificado"),
        new("J18.9", "Pneumonia não especificada"),
        new("J20.9", "Bronquite aguda não especificada"),
        new("J30.4", "Rinite alérgica não especificada"),
        new("J32.9", "Sinusite crônica não especificada"),
        new("J44.9", "Doença pulmonar obstrutiva crônica não especificada"),
        new("J45.9", "Asma não especificada"),
        new("K21.9", "Doença de refluxo gastroesofágico sem esofagite"),
        new("K29.7", "Gastrite não especificada"),
        new("K30", "Dispepsia"),
        new("K52.9", "Gastroenterite e colite não infecciosas, não especificadas"),
        new("K58.9", "Síndrome do cólon irritável sem diarreia"),
        new("K59.0", "Constipação"),
        new("K80.2", "Calculose da vesícula biliar sem colecistite"),
        new("L02.9", "Abscesso cutâneo, furúnculo e antraz, localização não especificada"),
        new("L20.9", "Dermatite atópica não especificada"),
        new("L23.9", "Dermatite alérgica de contato, causa não especificada"),
        new("L30.9", "Dermatite não especificada"),
        new("L50.9", "Urticária não especificada"),
        new("L70.0", "Acne vulgar"),
        new("M10.9", "Gota não especificada"),
        new("M17.9", "Gonartrose não especificada"),
        new("M25.5", "Dor articular"),
        new("M54.2", "Cervicalgia"),
        new("M54.4", "Lumbago com ciática"),
        new("M54.5", "Dor lombar baixa"),
        new("M65.9", "Sinovite e tenossinovite não especificadas"),
        new("M75.1", "Síndrome do manguito rotador"),
        new("M79.1", "Mialgia"),
        new("M79.7", "Fibromialgia"),
        new("N18.9", "Doença renal crônica não especificada"),
        new("N20.0", "Calculose do rim"),
        new("N30.0", "Cistite aguda"),
        new("N39.0", "Infecção do trato urinário de localização não especificada"),
        new("N40", "Hiperplasia da próstata"),
        new("N76.0", "Vaginite aguda"),
        new("N94.6", "Dismenorreia não especificada"),
        new("N95.1", "Estados da menopausa e do climatério feminino"),
        new("R05", "Tosse"),
        new("R06.0", "Dispneia"),
        new("R07.4", "Dor torácica não especificada"),
        new("R10.4", "Outras dores abdominais e as não especificadas"),
        new("R11", "Náusea e vômitos"),
        new("R42", "Tontura e instabilidade"),
        new("R50.9", "Febre não especificada"),
        new("R51", "Cefaleia"),
        new("R53", "Mal-estar, fadiga"),
        new("R60.0", "Edema localizado"),
        new("S93.4", "Entorse e distensão do tornozelo"),
        new("T78.4", "Alergia não especificada"),
        new("U07.1", "COVID-19, vírus identificado"),
        new("Z00.0", "Exame médico geral"),
        new("Z00.1", "Exame de rotina de saúde da criança"),
        new("Z01.4", "Exame ginecológico (geral) de rotina"),
        new("Z02.7", "Obtenção de atestado médico"),
        new("Z30.0", "Aconselhamento geral sobre contracepção"),
        new("Z32.1", "Gravidez confirmada"),
        new("Z34.9", "Supervisão de gravidez normal, não especificada"),
        new("Z71.9", "Aconselhamento não especificado"),
        new("Z76.0", "Emissão de prescrição de repetição")
    ];

    public static List<CidResponse> Buscar(string? termo, int limite = 20)
    {
        limite = Math.Clamp(limite, 1, 50);
        if (string.IsNullOrWhiteSpace(termo)) return Itens.Take(limite).ToList();

        var t = Normalizar(termo);
        var codigo = t.Replace(".", "").ToUpperInvariant();

        return Itens
            .Select(i => new
            {
                Item = i,
                // Código que começa com o termo vem primeiro; depois descrições que contêm o termo.
                Peso = i.Codigo.Replace(".", "").StartsWith(codigo, StringComparison.OrdinalIgnoreCase) ? 0
                     : Normalizar(i.Descricao).Contains(t) ? 1
                     : -1
            })
            .Where(x => x.Peso >= 0)
            .OrderBy(x => x.Peso)
            .ThenBy(x => x.Item.Codigo, StringComparer.Ordinal)
            .Take(limite)
            .Select(x => x.Item)
            .ToList();
    }

    private static string Normalizar(string texto)
    {
        var decomposto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposto.Length);
        foreach (var c in decomposto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) sb.Append(c);
        }
        return sb.ToString();
    }
}
