using Consultorio.Dominio.Entidades.Base;

namespace Consultorio.Dominio.Entidades;

/// <summary>
/// Trilha de acesso a dados sensíveis (LGPD): quem leu ou alterou o quê, quando e de onde.
/// Registros de auditoria nunca são alterados ou removidos pela aplicação.
/// </summary>
public class RegistroAuditoria : Entidade
{
    public string Usuario { get; private set; } = string.Empty;
    public string Acao { get; private set; } = string.Empty;
    public string Recurso { get; private set; } = string.Empty;
    public string? IdRecurso { get; private set; }
    public string Metodo { get; private set; } = string.Empty;
    public string Caminho { get; private set; } = string.Empty;
    public int StatusCode { get; private set; }
    public string? Ip { get; private set; }

    protected RegistroAuditoria() { }

    public RegistroAuditoria(string usuario, string acao, string recurso, string? idRecurso,
        string metodo, string caminho, int statusCode, string? ip)
    {
        Usuario = Limitar(string.IsNullOrWhiteSpace(usuario) ? "anônimo" : usuario, 256);
        Acao = Limitar(acao, 30);
        Recurso = Limitar(recurso, 60);
        IdRecurso = idRecurso is null ? null : Limitar(idRecurso, 60);
        Metodo = Limitar(metodo, 10);
        Caminho = Limitar(caminho, 400);
        StatusCode = statusCode;
        Ip = ip is null ? null : Limitar(ip, 64);
    }

    private static string Limitar(string valor, int tamanho) =>
        valor.Length <= tamanho ? valor : valor[..tamanho];
}
