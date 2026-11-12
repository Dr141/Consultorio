using Consultorio.Aplicacao.Configuracao;
using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Aplicacao.Servicos;

/// <summary>Dados da assinatura impressos no PDF (o selo visível; a assinatura criptográfica vai no arquivo).</summary>
public record InfoAssinaturaPdf(string Titular, DateTime DataUtc, bool IcpBrasil);

/// <summary>Monta o conteúdo do PDF de um documento clínico, no mesmo layout da página de impressão.</summary>
public static class DocumentoPdf
{
    public static ConteudoPdf Montar(DocumentoResponse d, OpcoesClinica opcoes, InfoAssinaturaPdf? assinatura = null)
    {
        var fuso = opcoes.ObterFuso();
        var emissao = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(d.DtEmissao, DateTimeKind.Utc), fuso);
        var blocos = new List<BlocoPdf> { new(d.NomeClinica, EstiloBlocoPdf.Cabecalho) };

        var contato = string.Join(" · ", new[] { d.EnderecoClinica, d.TelefoneClinica }.Where(v => !string.IsNullOrWhiteSpace(v)));
        if (contato.Length > 0) blocos.Add(new(contato, EstiloBlocoPdf.Pequeno));
        blocos.Add(new(string.Empty, EstiloBlocoPdf.Linha, 8));
        blocos.Add(new(d.TipoNome, EstiloBlocoPdf.Titulo, 16));
        if (d.Tipo == "ReceitaControleEspecial")
        {
            blocos.Add(new("Imprimir em duas vias: 1ª via retida na farmácia, 2ª via do paciente.", EstiloBlocoPdf.Centralizado));
        }

        var paciente = $"Paciente: {d.NomePaciente}";
        if (!string.IsNullOrWhiteSpace(d.CpfPaciente)) paciente += $"   ·   CPF: {d.CpfPaciente}";
        if (d.DtNascPaciente is { } nascimento) paciente += $"   ·   Idade: {Idade(nascimento, emissao)} anos";
        blocos.Add(new(paciente, EstiloBlocoPdf.Negrito, 14));

        if (d.Tipo == "PedidoExames")
        {
            blocos.Add(new("Solicito:", EstiloBlocoPdf.Negrito, 14));
            var itens = d.Conteudo.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(l => l.TrimStart('-', '•', '*', ' ').Trim())
                .Where(l => l.Length > 0)
                .Select((l, i) => $"{i + 1}. {l}");
            blocos.Add(new(string.Join("\n", itens), EstiloBlocoPdf.Normal, 4));
        }
        else
        {
            blocos.Add(new(d.Conteudo, EstiloBlocoPdf.Normal, 14));
        }

        if (d.DiasAfastamento is { } dias)
        {
            blocos.Add(new($"Período de afastamento: {dias} dia(s) a partir de {emissao:dd/MM/yyyy}.", EstiloBlocoPdf.Negrito, 10));
        }
        if (!string.IsNullOrWhiteSpace(d.CidCodigo))
        {
            blocos.Add(new($"CID-10: {d.CidCodigo} - {d.CidDescricao}", EstiloBlocoPdf.Negrito, 6));
        }

        blocos.Add(new(emissao.ToString("dd 'de' MMMM 'de' yyyy", Notificacoes.PtBr), EstiloBlocoPdf.Normal, 24));

        blocos.Add(new("220", EstiloBlocoPdf.Linha, 40));
        blocos.Add(new(d.NomeMedico, EstiloBlocoPdf.CentralizadoNegrito));
        var crm = $"CRM {d.CrmMedico}";
        if (d.EspecialidadesMedico.Count > 0) crm += " · " + string.Join(", ", d.EspecialidadesMedico);
        blocos.Add(new(crm, EstiloBlocoPdf.Centralizado));

        if (assinatura is not null)
        {
            var quando = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(assinatura.DataUtc, DateTimeKind.Utc), fuso);
            var padrao = assinatura.IcpBrasil ? "certificado digital ICP-Brasil" : "certificado digital";
            blocos.Add(new(
                $"Documento assinado digitalmente por {assinatura.Titular} em {quando:dd/MM/yyyy HH:mm}, com {padrao} " +
                "(MP 2.200-2/2001). Confira a assinatura em validar.iti.gov.br.",
                EstiloBlocoPdf.Centralizado, 14));
        }

        var rodape = $"Código de verificação {d.CodigoVerificacao} · confira a autenticidade em {d.UrlVerificacao}";
        return new ConteudoPdf($"{d.TipoNome} - {d.NomePaciente}", blocos, rodape, d.Cancelado ? "CANCELADO" : null);
    }

    public static string NomeArquivo(DocumentoResponse d, bool assinado) =>
        $"{d.Tipo.ToLowerInvariant()}-{d.CodigoVerificacao}{(assinado ? "-assinado" : "")}.pdf";

    private static int Idade(DateTime nascimento, DateTime referencia)
    {
        var idade = referencia.Year - nascimento.Year;
        if (referencia.Date < nascimento.Date.AddYears(idade)) idade--;
        return Math.Max(0, idade);
    }
}
