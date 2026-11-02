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
            .Include(e => e.Agenda)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Exame>> ObterTodosDetalhado()
    {
        return await _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Agenda)
            .OrderByDescending(e => e.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Exame>> ObterPorPaciente(int idPaciente)
    {
        return await _contexto.Exame
            .Include(e => e.Paciente).ThenInclude(p => p.Pessoa)
            .Include(e => e.Agenda)
            .Where(e => e.IdPaciente == idPaciente)
            .OrderByDescending(e => e.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
