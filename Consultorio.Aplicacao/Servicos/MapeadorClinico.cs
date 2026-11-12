using Consultorio.Aplicacao.DTOs.Request;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>Conversões entre DTOs e objetos de valor clínicos, compartilhadas pelos serviços.</summary>
public static class MapeadorClinico
{
    public static SinaisVitais? CriarSinaisVitais(SinaisVitaisDto? dto) =>
        dto is null
            ? null
            : new SinaisVitais(dto.PressaoSistolica, dto.PressaoDiastolica, dto.FrequenciaCardiaca, dto.FrequenciaRespiratoria,
                dto.Temperatura, dto.SaturacaoO2, dto.PesoKg, dto.AlturaCm, dto.Glicemia);

    public static SinaisVitaisResponse? MapearSinaisVitais(SinaisVitais? sv) =>
        sv is null || sv.Vazio
            ? null
            : new SinaisVitaisResponse(sv.PressaoSistolica, sv.PressaoDiastolica, sv.FrequenciaCardiaca, sv.FrequenciaRespiratoria,
                sv.Temperatura, sv.SaturacaoO2, sv.PesoKg, sv.AlturaCm, sv.Glicemia, sv.Imc);

    /// <summary>CID a partir do código; sem descrição informada, usa a do catálogo quando houver.</summary>
    public static Cid? CriarCid(string? codigo, string? descricao)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var cid = new Cid(codigo, descricao);
        if (!string.IsNullOrWhiteSpace(cid.Descricao)) return cid;
        var doCatalogo = CatalogoCid.Buscar(cid.Codigo, 1).FirstOrDefault(c => c.Codigo == cid.Codigo);
        return new Cid(cid.Codigo, doCatalogo?.Descricao);
    }
}
