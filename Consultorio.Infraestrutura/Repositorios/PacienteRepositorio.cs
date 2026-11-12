using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class PacienteRepositorio(ConsultorioContexto contexto) : Repositorio<Paciente>(contexto), IPacienteRepositorio
{
    public async Task<Paciente?> ObterDetalhadoPorId(int id)
    {
        return await _contexto.Paciente
            .Include(p => p.Pessoa)
            .Include(p => p.Convenio)
            .Include(p => p.Consultas)
            .Include(p => p.Exames)
            .Include(p => p.Agendas)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<List<Paciente>> ObterTodosDetalhado()
    {
        return await _contexto.Paciente
            .Include(p => p.Pessoa)
            .Include(p => p.Convenio)
            .OrderBy(p => p.Pessoa.Nome)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Paciente?> ObterPorIdPessoa(int idPessoa)
    {
        return await _contexto.Paciente
            .Include(p => p.Pessoa)
            .FirstOrDefaultAsync(p => p.IdPessoa == idPessoa);
    }
}
