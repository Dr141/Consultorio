using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ConsultaRepositorio(ConsultorioContexto contexto) : Repositorio<Consulta>(contexto), IConsultaRepositorio
{
    public async Task<Consulta?> ObterDetalhadoPorId(int id)
    {
        return await _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
            .Include(c => c.Adendos)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Consulta>> ObterTodosDetalhado()
    {
        return await _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
            .Include(c => c.Adendos)
            .OrderByDescending(c => c.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Consulta>> ObterPorPaciente(int idPaciente)
    {
        return await _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
            .Include(c => c.Adendos)
            .Where(c => c.IdPaciente == idPaciente)
            .OrderByDescending(c => c.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Consulta>> ObterPorMedico(int idMedico)
    {
        return await _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
            .Include(c => c.Adendos)
            .Where(c => c.IdMedico == idMedico)
            .OrderByDescending(c => c.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Consulta>> ObterPorPacienteFiltrado(
        int idPaciente,
        int? idMedico = null,
        DateTime? dataInicio = null,
        DateTime? dataFim = null)
    {
        var query = _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
            .Include(c => c.Adendos)
            .Where(c => c.IdPaciente == idPaciente);

        if (idMedico.HasValue && idMedico.Value > 0)
        {
            query = query.Where(c => c.IdMedico == idMedico.Value);
        }

        if (dataInicio.HasValue)
        {
            var dtIniUtc = DateTime.SpecifyKind(dataInicio.Value.Date, DateTimeKind.Utc);
            query = query.Where(c => (c.Agenda != null && c.Agenda.Data >= dtIniUtc) || (c.Agenda == null && c.DtCriacao >= dtIniUtc));
        }

        if (dataFim.HasValue)
        {
            var dtFimUtc = DateTime.SpecifyKind(dataFim.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(c => (c.Agenda != null && c.Agenda.Data <= dtFimUtc) || (c.Agenda == null && c.DtCriacao <= dtFimUtc));
        }

        return await query
            .OrderByDescending(c => c.Agenda != null ? c.Agenda.Data : c.DtCriacao)
            .ThenByDescending(c => c.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
