using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class AgendaRepositorio(ConsultorioContexto contexto) : Repositorio<Agenda>(contexto), IAgendaRepositorio
{
    public async Task<Agenda?> ObterDetalhadoPorId(int id)
    {
        return await _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .Include(a => a.Consulta)
            .Include(a => a.Exame)
            .FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<List<Agenda>> ObterTodosDetalhado()
    {
        return await _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .OrderBy(a => a.Data).ThenBy(a => a.Hora)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Agenda>> ObterPorMedicoEData(int idMedico, DateTime data)
    {
        var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        return await _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .Where(a => a.IdMedico == idMedico && a.Data == dataUtc)
            .OrderBy(a => a.Hora)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Agenda>> ObterPorPaciente(int idPaciente)
    {
        return await _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .Where(a => a.IdPaciente == idPaciente)
            .OrderByDescending(a => a.Data).ThenByDescending(a => a.Hora)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ExisteConflitoHorario(int idMedico, DateTime data, TimeSpan hora, int? excluirId = null)
    {
        var dataUtc = DateTime.SpecifyKind(data.Date, DateTimeKind.Utc);
        var query = _contexto.Agenda
            .Where(a => a.IdMedico == idMedico && a.Data == dataUtc && a.Hora == hora);

        if (excluirId.HasValue)
        {
            query = query.Where(a => a.Id != excluirId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<List<Agenda>> ObterPorPeriodo(DateTime inicio, DateTime fim, int? idMedico = null)
    {
        var ini = DateTime.SpecifyKind(inicio.Date, DateTimeKind.Utc);
        var f = DateTime.SpecifyKind(fim.Date, DateTimeKind.Utc);
        var query = _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .Where(a => a.Data >= ini && a.Data <= f);

        if (idMedico.HasValue && idMedico.Value > 0)
            query = query.Where(a => a.IdMedico == idMedico.Value);

        return await query.OrderBy(a => a.Data).ThenBy(a => a.Hora).ToListAsync();
    }

    public async Task<Agenda?> ObterPorToken(Guid token)
    {
        return await _contexto.Agenda
            .Include(a => a.Medico).ThenInclude(m => m!.Pessoa)
            .Include(a => a.Paciente).ThenInclude(p => p!.Pessoa)
            .FirstOrDefaultAsync(a => a.TokenConfirmacao == token);
    }

    public async Task AdicionarVarios(IEnumerable<Agenda> agendas)
    {
        await _contexto.Agenda.AddRangeAsync(agendas);
        await _contexto.SaveChangesAsync();
    }

    public async Task RemoverVarios(IEnumerable<Agenda> agendas)
    {
        _contexto.Agenda.RemoveRange(agendas);
        await _contexto.SaveChangesAsync();
    }
}
