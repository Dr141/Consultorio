using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IAuditoriaRepositorio : IRepositorio<RegistroAuditoria>
{
    Task<List<RegistroAuditoria>> ObterFiltrado(
        DateTime? inicio = null,
        DateTime? fim = null,
        string? usuario = null,
        string? recurso = null,
        string? idRecurso = null,
        int limite = 500);
}
