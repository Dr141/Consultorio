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
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Consulta>> ObterTodosDetalhado()
    {
        return await _contexto.Consulta
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Medico).ThenInclude(m => m.Pessoa)
            .Include(c => c.Agenda)
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
            .Where(c => c.IdMedico == idMedico)
            .OrderByDescending(c => c.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
