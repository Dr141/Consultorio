using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Arquivo do resultado de um exame (PDF ou imagem do laboratório).
/// Faz parte do prontuário e não é removido. O conteúdo é validado pelos primeiros bytes do arquivo,
/// não só pela extensão, para impedir o envio de outros tipos disfarçados.
/// </summary>
public class ExameAnexo : Entidade
{
    public const long TamanhoMaximoBytes = 10 * 1024 * 1024;

    public int IdExame { get; private set; }
    public string NomeArquivo { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long TamanhoBytes { get; private set; }
    public byte[] Conteudo { get; private set; } = [];
    public string EnviadoPor { get; private set; } = string.Empty;

    protected ExameAnexo() { }

    public ExameAnexo(int idExame, string nomeArquivo, byte[] conteudo, string enviadoPor)
    {
        DomainException.When(idExame <= 0, "Exame inválido.");
        DomainException.When(conteudo is null || conteudo.Length == 0, "O arquivo está vazio.");
        DomainException.When(conteudo!.LongLength > TamanhoMaximoBytes, "O arquivo ultrapassa o limite de 10 MB.");

        var tipo = DetectarTipo(conteudo)
            ?? throw new DomainException("Formato não suportado. Envie PDF, JPG ou PNG.");

        var nome = Path.GetFileName(nomeArquivo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome)) nome = $"resultado{tipo.Extensao}";
        if (nome.Length > 200) nome = nome[..200];

        IdExame = idExame;
        NomeArquivo = nome;
        ContentType = tipo.ContentType;
        TamanhoBytes = conteudo.LongLength;
        Conteudo = conteudo;
        EnviadoPor = string.IsNullOrWhiteSpace(enviadoPor) ? "desconhecido" : enviadoPor.Trim();
    }

    /// <summary>Versão sem o conteúdo binário, para listagens.</summary>
    public static ExameAnexo Resumo(int id, int idExame, string nomeArquivo, string contentType, long tamanhoBytes, string enviadoPor, DateTime dtCriacao) =>
        new()
        {
            Id = id,
            IdExame = idExame,
            NomeArquivo = nomeArquivo,
            ContentType = contentType,
            TamanhoBytes = tamanhoBytes,
            EnviadoPor = enviadoPor,
            DtCriacao = dtCriacao
        };

    private record TipoArquivo(string ContentType, string Extensao);

    private static TipoArquivo? DetectarTipo(byte[] dados)
    {
        if (dados.Length >= 4 && dados[0] == 0x25 && dados[1] == 0x50 && dados[2] == 0x44 && dados[3] == 0x46)
            return new("application/pdf", ".pdf"); // %PDF
        if (dados.Length >= 8 && dados[0] == 0x89 && dados[1] == 0x50 && dados[2] == 0x4E && dados[3] == 0x47)
            return new("image/png", ".png");
        if (dados.Length >= 3 && dados[0] == 0xFF && dados[1] == 0xD8 && dados[2] == 0xFF)
            return new("image/jpeg", ".jpg");
        return null;
    }
}
