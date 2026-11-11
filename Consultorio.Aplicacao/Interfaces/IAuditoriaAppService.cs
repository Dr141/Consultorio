using Consultorio.Aplicacao.DTOs.Response;

namespace Consultorio.Aplicacao.Interfaces;

public interface IAuditoriaAppService
{
    Task RegistrarAsync(string usuario, string acao, string recurso, string? idRecurso,
        string metodo, string caminho, int statusCode, string? ip);

    Task<List<RegistroAuditoriaResponse>> ObterAsync(
        DateTime? inicio = null, DateTime? fim = null, string? usuario = null,
        string? recurso = null, string? idRecurso = null, int limite = 500);
}
