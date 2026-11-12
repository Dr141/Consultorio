using System.Security.Cryptography;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.ValueObjects;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Documento emitido ao paciente (receita, atestado, pedido de exames...).
/// Faz parte do prontuário: não é editado nem apagado; se emitido por engano, é cancelado com motivo.
/// O código de verificação permite a terceiros (empresa, farmácia) conferir a autenticidade.
/// </summary>
public class DocumentoClinico : Entidade
{
    public const int TamanhoMaximoConteudo = 8000;
    private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // sem O/0/I/1, que se confundem

    public TipoDocumentoClinico Tipo { get; private set; }
    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;
    public int IdMedico { get; private set; }
    public Medico Medico { get; private set; } = null!;
    public int? IdConsulta { get; private set; }

    public string Conteudo { get; private set; } = string.Empty;
    public int? DiasAfastamento { get; private set; }
    public string? CidCodigo { get; private set; }
    public string? CidDescricao { get; private set; }

    public string CodigoVerificacao { get; private set; } = string.Empty;
    public string EmitidoPor { get; private set; } = string.Empty;

    public bool Cancelado { get; private set; }
    public DateTime? DtCancelamento { get; private set; }
    public string? MotivoCancelamento { get; private set; }

    /// <summary>Preenchidos quando o médico assina o PDF com certificado digital (o arquivo fica em DocumentoAssinatura).</summary>
    public DateTime? DtAssinatura { get; private set; }
    public string? AssinadoPor { get; private set; }
    public bool Assinado => DtAssinatura.HasValue;

    protected DocumentoClinico() { }

    public DocumentoClinico(TipoDocumentoClinico tipo, int idPaciente, int idMedico, string conteudo, string emitidoPor,
        int? idConsulta = null, int? diasAfastamento = null, Cid? cid = null)
    {
        DomainException.When(!Enum.IsDefined(tipo), "Tipo de documento inválido.");
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        DomainException.When(idMedico <= 0, "Médico inválido.");
        DomainException.When(string.IsNullOrWhiteSpace(conteudo), "O conteúdo do documento é obrigatório.");
        DomainException.When(conteudo.Trim().Length > TamanhoMaximoConteudo, $"O documento não pode ultrapassar {TamanhoMaximoConteudo} caracteres.");
        DomainException.When(string.IsNullOrWhiteSpace(emitidoPor), "Usuário emissor não identificado.");

        if (tipo == TipoDocumentoClinico.Atestado)
        {
            DomainException.When(diasAfastamento is null or < 1 or > 365, "Informe os dias de afastamento do atestado (1 a 365).");
        }
        else
        {
            DomainException.When(diasAfastamento.HasValue, "Dias de afastamento só se aplicam a atestados.");
        }

        Tipo = tipo;
        IdPaciente = idPaciente;
        IdMedico = idMedico;
        IdConsulta = idConsulta;
        Conteudo = conteudo.Trim();
        DiasAfastamento = diasAfastamento;
        // O CID só sai no documento se o médico incluir (no atestado, depende de autorização do paciente).
        CidCodigo = cid?.Codigo;
        CidDescricao = cid?.Descricao;
        EmitidoPor = emitidoPor.Trim();
        CodigoVerificacao = GerarCodigo();
    }

    public void Cancelar(string motivo)
    {
        DomainException.When(Cancelado, "O documento já está cancelado.");
        DomainException.When(string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length < 5, "Informe o motivo do cancelamento (mínimo de 5 caracteres).");
        DomainException.When(motivo.Trim().Length > 500, "O motivo do cancelamento não pode ultrapassar 500 caracteres.");
        Cancelado = true;
        DtCancelamento = DateTime.UtcNow;
        MotivoCancelamento = motivo.Trim();
    }

    public void RegistrarAssinatura(string titularCertificado, DateTime dtAssinaturaUtc)
    {
        DomainException.When(Cancelado, "Documento cancelado não pode ser assinado.");
        DomainException.When(Assinado, "O documento já foi assinado digitalmente.");
        DomainException.When(string.IsNullOrWhiteSpace(titularCertificado), "Titular do certificado não identificado.");
        var titular = titularCertificado.Trim();
        AssinadoPor = titular.Length > 200 ? titular[..200] : titular;
        DtAssinatura = DateTime.SpecifyKind(dtAssinaturaUtc, DateTimeKind.Utc);
    }

    /// <summary>Linhas do pedido de exames (um exame por linha).</summary>
    public IReadOnlyList<string> ItensDoPedido() =>
        Conteudo.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.TrimStart('-', '•', '*', ' ').Trim())
            .Where(l => l.Length >= 2)
            .ToList();

    private static string GerarCodigo()
    {
        Span<char> codigo = stackalloc char[10];
        for (var i = 0; i < codigo.Length; i++)
        {
            codigo[i] = AlfabetoCodigo[RandomNumberGenerator.GetInt32(AlfabetoCodigo.Length)];
        }
        return new string(codigo);
    }
}
