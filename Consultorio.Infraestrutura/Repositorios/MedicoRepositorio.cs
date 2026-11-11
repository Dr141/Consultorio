using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class MedicoRepositorio(ConsultorioContexto contexto) : Repositorio<Medico>(contexto), IMedicoRepositorio
{
    public async Task<Medico?> ObterDetalhadoPorId(int id)
    {
        return await _contexto.Medico
            .Include(m => m.Pessoa)
            .Include(m => m.Consultas)
            .Include(m => m.Agendas)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Medico>> ObterTodosDetalhado()
    {
        return await _contexto.Medico
            .Include(m => m.Pessoa)
            .OrderBy(m => m.Pessoa.Nome)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Medico?> ObterPorCrm(string crm)
    {
        var crmVo = new Crm(crm);
        return await _contexto.Medico
            .Include(m => m.Pessoa)
            .FirstOrDefaultAsync(m => m.Crm == crmVo);
    }

    public async Task<bool> ExisteCrm(string crm, int? excluirId = null)
    {
        var crmVo = new Crm(crm);
        var query = _contexto.Medico.Where(m => m.Crm == crmVo);
        if (excluirId.HasValue)
        {
            query = query.Where(m => m.Id != excluirId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Medico?> ObterPorIdPessoa(int idPessoa)
    {
        return await _contexto.Medico
            .Include(m => m.Pessoa)
            .FirstOrDefaultAsync(m => m.IdPessoa == idPessoa);
    }

    public async Task<Medico?> ObterPorEmailUsuario(string email)
    {
        var normalizado = email.Trim().ToLowerInvariant();
        return await _contexto.Medico
            .Include(m => m.Pessoa)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.EmailUsuario == normalizado);
    }
}
