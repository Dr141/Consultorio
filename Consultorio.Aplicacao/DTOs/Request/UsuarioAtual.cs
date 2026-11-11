namespace Consultorio.Aplicacao.DTOs.Request;

/// <summary>
/// Quem está executando a operação, extraído do token pelo controller.
/// Usado para regras de autoria (ex.: só o médico responsável altera a própria consulta).
/// </summary>
public record UsuarioAtual(string Email, bool EhAdministrador)
{
    public string Identificacao => string.IsNullOrWhiteSpace(Email) ? "desconhecido" : Email;
}
