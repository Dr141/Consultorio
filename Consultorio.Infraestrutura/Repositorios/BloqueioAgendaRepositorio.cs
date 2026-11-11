using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class BloqueioAgendaRepositorio(ConsultorioContexto contexto) : Repositorio<BloqueioAgenda>(contexto), IBloqueioAgendaRepositorio
{
    public async Task<List<BloqueioAgenda>> ObterNoPeriodo(DateTime inicio, DateTime fim, int? idMedico = null)
    {
        var ini = DateTime.SpecifyKind(inicio.Date, DateTimeKind.Utc);
        var f = DateTime.SpecifyKind(fim.Date, DateTimeKind.Utc);
        var query = _contexto.BloqueioAgenda
            .Include(b => b.Medico).ThenInclude(m => m!.Pessoa)
            .Where(b => b.DataInicio <= f && b.DataFim >= ini);

        if (idMedico.HasValue && idMedico.Value > 0)
            query = query.Where(b => b.IdMedico == null || b.IdMedico == idMedico.Value);

        return await query.AsNoTracking().OrderBy(b => b.DataInicio).ToListAsync();
    }

    public async Task<List<BloqueioAgenda>> ObterAPartirDe(DateTime data)
    {
        var d = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        return await _contexto.BloqueioAgenda
            .Include(b => b.Medico).ThenInclude(m => m!.Pessoa)
            .Where(b => b.DataFim >= d)
            .OrderBy(b => b.DataInicio)
            .AsNoTracking()
            .ToListAsync();
    }
}
