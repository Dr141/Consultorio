using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class AuditoriaRepositorio(ConsultorioContexto contexto) : Repositorio<RegistroAuditoria>(contexto), IAuditoriaRepositorio
{
    public override Task Remover(RegistroAuditoria entidade) =>
        throw new InvalidOperationException("Registros de auditoria não podem ser removidos.");

    public async Task<Dictionary<string, DateTime>> UltimaAtividadePorUsuario()
    {
        var ultimas = await _contexto.RegistroAuditoria
            .AsNoTracking()
            .GroupBy(r => r.Usuario.ToLower())
            .Select(g => new { Usuario = g.Key, Ultima = g.Max(r => r.DtCriacao) })
            .ToListAsync();
        return ultimas.ToDictionary(u => u.Usuario, u => u.Ultima);
    }

    public async Task<List<RegistroAuditoria>> ObterFiltrado(
        DateTime? inicio = null,
        DateTime? fim = null,
        string? usuario = null,
        string? recurso = null,
        string? idRecurso = null,
        int limite = 500)
    {
        var query = _contexto.RegistroAuditoria.AsNoTracking().AsQueryable();

        if (inicio.HasValue)
        {
            var ini = DateTime.SpecifyKind(inicio.Value, DateTimeKind.Utc);
            query = query.Where(r => r.DtCriacao >= ini);
        }

        if (fim.HasValue)
        {
            var f = DateTime.SpecifyKind(fim.Value, DateTimeKind.Utc);
            query = query.Where(r => r.DtCriacao <= f);
        }

        if (!string.IsNullOrWhiteSpace(usuario))
        {
            var termo = usuario.Trim().ToLower();
            query = query.Where(r => r.Usuario.ToLower().Contains(termo));
        }

        if (!string.IsNullOrWhiteSpace(recurso))
            query = query.Where(r => r.Recurso == recurso);

        if (!string.IsNullOrWhiteSpace(idRecurso))
            query = query.Where(r => r.IdRecurso == idRecurso);

        return await query
            .OrderByDescending(r => r.DtCriacao)
            .Take(Math.Clamp(limite, 1, 2000))
            .ToListAsync();
    }
}
