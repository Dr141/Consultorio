using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;
using Consultorio.Dominio.Interfaces.Servicos;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Certificado digital A1 do médico, usado para assinar receitas e atestados em PDF (padrão ICP-Brasil).
/// O arquivo .pfx e a senha ficam criptografados no banco; só um certificado ativo por médico.
/// Ao ser substituído ou removido, os segredos são apagados e fica só o histórico (titular, validade).
/// </summary>
public class CertificadoDigital : Entidade
{
    public const int TamanhoMaximoBytes = 50 * 1024;

    public int IdMedico { get; private set; }
    public string Titular { get; private set; } = string.Empty;
    public string Emissor { get; private set; } = string.Empty;
    public string NumeroSerie { get; private set; } = string.Empty;
    public string Thumbprint { get; private set; } = string.Empty;
    public DateTime ValidoDe { get; private set; }
    public DateTime ValidoAte { get; private set; }
    public bool IcpBrasil { get; private set; }
    public byte[] PfxProtegido { get; private set; } = [];
    public byte[] SenhaProtegida { get; private set; } = [];
    public string EnviadoPor { get; private set; } = string.Empty;
    public bool Ativo { get; private set; } = true;
    public DateTime? DtDesativacao { get; private set; }

    protected CertificadoDigital() { }

    public CertificadoDigital(int idMedico, DadosCertificado dados, byte[] pfxProtegido, byte[] senhaProtegida, string enviadoPor, DateTime agoraUtc)
    {
        DomainException.When(idMedico <= 0, "Médico inválido.");
        DomainException.When(dados is null, "Certificado inválido.");
        DomainException.When(!dados!.PossuiChavePrivada,
            "O arquivo não contém a chave privada. Exporte o certificado A1 como .pfx/.p12 incluindo a chave privada.");
        DomainException.When(dados.ValidoAteUtc <= agoraUtc, $"Certificado vencido em {dados.ValidoAteUtc:dd/MM/yyyy}.");
        DomainException.When(dados.ValidoDeUtc > agoraUtc, $"Certificado válido somente a partir de {dados.ValidoDeUtc:dd/MM/yyyy}.");
        DomainException.When(pfxProtegido is null || pfxProtegido.Length == 0 || senhaProtegida is null, "Certificado inválido.");

        IdMedico = idMedico;
        Titular = Limitar(dados.Titular, 200);
        Emissor = Limitar(dados.Emissor, 300);
        NumeroSerie = Limitar(dados.NumeroSerie, 80);
        Thumbprint = Limitar(dados.Thumbprint, 80);
        ValidoDe = DateTime.SpecifyKind(dados.ValidoDeUtc, DateTimeKind.Utc);
        ValidoAte = DateTime.SpecifyKind(dados.ValidoAteUtc, DateTimeKind.Utc);
        IcpBrasil = dados.IcpBrasil;
        PfxProtegido = pfxProtegido!;
        SenhaProtegida = senhaProtegida!;
        EnviadoPor = string.IsNullOrWhiteSpace(enviadoPor) ? "desconhecido" : Limitar(enviadoPor.Trim(), 256);
    }

    public bool VigenteEm(DateTime agoraUtc) => Ativo && ValidoDe <= agoraUtc && agoraUtc < ValidoAte;

    /// <summary>Desativa e apaga o arquivo e a senha; o registro fica como histórico.</summary>
    public void Desativar(DateTime agoraUtc)
    {
        DomainException.When(!Ativo, "O certificado já foi removido.");
        Ativo = false;
        DtDesativacao = DateTime.SpecifyKind(agoraUtc, DateTimeKind.Utc);
        PfxProtegido = [];
        SenhaProtegida = [];
    }

    private static string Limitar(string? valor, int tamanho)
    {
        var texto = (valor ?? string.Empty).Trim();
        return texto.Length <= tamanho ? texto : texto[..tamanho];
    }
}
