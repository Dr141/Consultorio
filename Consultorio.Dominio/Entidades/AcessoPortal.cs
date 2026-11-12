using System.Security.Cryptography;
using System.Text;
using Consultorio.Dominio.Entidades.Base;
using Consultorio.Dominio.Excecoes;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Acesso do paciente ao portal, sem senha: um código de 6 dígitos é enviado ao e-mail cadastrado
/// e, validado, vira uma sessão curta. Código e sessão são guardados só como hash.
/// </summary>
public class AcessoPortal : Entidade
{
    public const int MaximoTentativas = 5;

    public int IdPaciente { get; private set; }
    public Paciente Paciente { get; private set; } = null!;

    /// <summary>Identificador público da solicitação (o cliente envia junto com o código).</summary>
    public Guid Chave { get; private set; }
    public string Salt { get; private set; } = string.Empty;
    public string CodigoHash { get; private set; } = string.Empty;
    public DateTime CodigoExpiraEm { get; private set; }
    public int Tentativas { get; private set; }

    public string? TokenHash { get; private set; }
    public DateTime? TokenExpiraEm { get; private set; }
    public DateTime? DtEncerramento { get; private set; }
    public string? Ip { get; private set; }

    protected AcessoPortal() { }

    private AcessoPortal(int idPaciente, string codigo, DateTime agoraUtc, TimeSpan validadeCodigo, string? ip)
    {
        DomainException.When(idPaciente <= 0, "Paciente inválido.");
        IdPaciente = idPaciente;
        Chave = Guid.NewGuid();
        Salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        CodigoHash = Hash(Salt + codigo);
        CodigoExpiraEm = agoraUtc.Add(validadeCodigo);
        Ip = ip is { Length: > 64 } ? ip[..64] : ip;
    }

    /// <summary>Cria a solicitação e devolve o código em claro (para enviar ao paciente).</summary>
    public static (AcessoPortal acesso, string codigo) Solicitar(int idPaciente, DateTime agoraUtc, TimeSpan validadeCodigo, string? ip = null)
    {
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return (new AcessoPortal(idPaciente, codigo, agoraUtc, validadeCodigo, ip), codigo);
    }

    /// <summary>Confere o código. Cada erro consome uma tentativa; esgotadas, a solicitação não vale mais.</summary>
    public bool ValidarCodigo(string? codigo, DateTime agoraUtc)
    {
        if (TokenHash is not null || DtEncerramento is not null) return false;
        if (agoraUtc > CodigoExpiraEm || Tentativas >= MaximoTentativas) return false;

        var informado = new string((codigo ?? string.Empty).Where(char.IsDigit).ToArray());
        var ok = informado.Length == 6 &&
                 CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Hash(Salt + informado)), Encoding.ASCII.GetBytes(CodigoHash));
        if (!ok) Tentativas++;
        return ok;
    }

    public bool TentativasEsgotadas => Tentativas >= MaximoTentativas;

    /// <summary>Abre a sessão e devolve o token em claro (só o hash fica guardado).</summary>
    public string AbrirSessao(DateTime agoraUtc, TimeSpan duracao)
    {
        DomainException.When(TokenHash is not null, "Esta solicitação já foi utilizada.");
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        TokenHash = HashToken(token);
        TokenExpiraEm = agoraUtc.Add(duracao);
        return token;
    }

    public bool SessaoValida(DateTime agoraUtc) =>
        TokenHash is not null && DtEncerramento is null && TokenExpiraEm.HasValue && agoraUtc < TokenExpiraEm.Value;

    public void Encerrar(DateTime agoraUtc) => DtEncerramento ??= agoraUtc;

    public static string HashToken(string token) => Hash("portal:" + token);

    private static string Hash(string valor) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));
}
