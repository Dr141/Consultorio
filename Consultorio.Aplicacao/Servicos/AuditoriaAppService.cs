using Consultorio.Aplicacao.DTOs.Response;
using Consultorio.Aplicacao.Interfaces;
using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;

namespace Consultorio.Aplicacao.Servicos;

public class AuditoriaAppService(IAuditoriaRepositorio auditoriaRepositorio) : IAuditoriaAppService
{
    public async Task RegistrarAsync(string usuario, string acao, string recurso, string? idRecurso,
        string metodo, string caminho, int statusCode, string? ip)
    {
        var registro = new RegistroAuditoria(usuario, acao, recurso, idRecurso, metodo, caminho, statusCode, ip);
        await auditoriaRepositorio.Adicionar(registro);
    }

    public async Task<List<RegistroAuditoriaResponse>> ObterAsync(
        DateTime? inicio = null, DateTime? fim = null, string? usuario = null,
        string? recurso = null, string? idRecurso = null, int limite = 500)
    {
        var registros = await auditoriaRepositorio.ObterFiltrado(inicio, fim, usuario, recurso, idRecurso, limite);
        return registros
            .Select(r => new RegistroAuditoriaResponse(
                r.Id, r.DtCriacao, r.Usuario, r.Acao, r.Recurso, r.IdRecurso, r.Metodo, r.Caminho, r.StatusCode, r.Ip))
            .ToList();
    }
}
