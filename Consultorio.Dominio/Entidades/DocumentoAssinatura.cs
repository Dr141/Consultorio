using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// PDF assinado digitalmente de um documento clínico. Fica em tabela própria para que o arquivo
/// não seja carregado junto com o documento. Uma assinatura por documento; não é alterada nem removida.
/// </summary>
public class DocumentoAssinatura : Entidade
{
    public int IdDocumento { get; private set; }
    public byte[] Pdf { get; private set; } = [];
    public string Titular { get; private set; } = string.Empty;
    public string Thumbprint { get; private set; } = string.Empty;
    public string AssinadoPor { get; private set; } = string.Empty;

    protected DocumentoAssinatura() { }

    public DocumentoAssinatura(int idDocumento, byte[] pdf, string titular, string thumbprint, string assinadoPor)
    {
        DomainException.When(idDocumento <= 0, "Documento inválido.");
        DomainException.When(pdf is null || pdf.Length < 8, "PDF assinado inválido.");
        IdDocumento = idDocumento;
        Pdf = pdf!;
        Titular = (titular ?? string.Empty).Trim();
        Thumbprint = (thumbprint ?? string.Empty).Trim();
        AssinadoPor = string.IsNullOrWhiteSpace(assinadoPor) ? "desconhecido" : assinadoPor.Trim();
    }
}
