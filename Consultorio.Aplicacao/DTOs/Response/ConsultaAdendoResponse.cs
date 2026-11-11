namespace Consultorio.Aplicacao.DTOs.Response;

public record ConsultaAdendoResponse(
    int Id,
    string Texto,
    string Autor,
    DateTime DtCriacao
);
