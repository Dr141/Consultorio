using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ExameRepositorio(ConsultorioContexto contexto) : Repositorio<Exame>(contexto), IExameRepositorio
{
    public async Task<Exame?> ObterDetalhadoPorId(int id)
    {
        return await _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Medico).ThenInclude(m => m!.Pessoa)
            .Include(e => e.Agenda).ThenInclude(a => a!.Medico).ThenInclude(m => m!.Pessoa)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Exame>> ObterTodosDetalhado()
    {
        return await _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Medico).ThenInclude(m => m!.Pessoa)
            .Include(e => e.Agenda).ThenInclude(a => a!.Medico).ThenInclude(m => m!.Pessoa)
            .OrderByDescending(e => e.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Exame>> ObterPorPaciente(int idPaciente)
    {
        return await _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Medico).ThenInclude(m => m!.Pessoa)
            .Include(e => e.Agenda).ThenInclude(a => a!.Medico).ThenInclude(m => m!.Pessoa)
            .Where(e => e.IdPaciente == idPaciente)
            .OrderByDescending(e => e.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Exame>> ObterPorPacienteFiltrado(
        int idPaciente,
        int? idMedico = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        var query = _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Medico).ThenInclude(m => m!.Pessoa)
            .Include(e => e.Agenda).ThenInclude(a => a!.Medico).ThenInclude(m => m!.Pessoa)
            .Where(e => e.IdPaciente == idPaciente);

        if (idMedico.HasValue && idMedico.Value > 0)
        {
            query = query.Where(e => e.IdMedico == idMedico.Value || (e.IdMedico == null && e.Agenda != null && e.Agenda.IdMedico == idMedico.Value));
        }

        if (dataInicio.HasValue)
        {
            var dtIniUtc = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => (e.Agenda != null && e.Agenda.Data >= dtIniUtc) || (e.Agenda == null && e.DtCriacao >= dtIniUtc));
        }

        if (dataFim.HasValue)
        {
            var dtFimUtc = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => (e.Agenda != null && e.Agenda.Data <= dtFimUtc) || (e.Agenda == null && e.DtCriacao <= dtFimUtc));
        }

        return await query
            .OrderByDescending(e => e.Agenda != null ? e.Agenda.Data : e.DtCriacao)
            .ThenByDescending(e => e.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
