namespace Consultorio.Aplicacao.DTOs.Response;

public record RegistroAuditoriaResponse(
    int Id,
    DateTime DtOcorrencia,
    string Usuario,
    string Acao,
    string Recurso,
    string? IdRecurso,
    string Metodo,
    string Caminho,
    int StatusCode,
    string? Ip
);
