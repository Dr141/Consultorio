using Consultorio.Dominio.Enumerados;

namespace Consultorio.Aplicacao.DTOs.Response;

public record MedicoResponse(
    int Id,
    int IdPessoa,
    string Nome,
    string Cpf,
    DateTime DtNasc,
    string Telefone,
    string Crm,
    List<Especialidade> Especialidades,
    DateTime DtCriacao
);
