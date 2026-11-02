using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Dominio.ValueObjects;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class PessoaRepositorio(ConsultorioContexto contexto) : Repositorio<Pessoa>(contexto), IPessoaRepositorio
{
    public async Task<Pessoa?> ObterPorCpf(string cpf)
    {
        var cpfVo = new Cpf(cpf);
        return await _contexto.Pessoa.FirstOrDefaultAsync(p => p.Cpf == cpfVo);
    }

    public async Task<bool> ExisteCpf(string cpf, int? excluirId = null)
    {
        var cpfVo = new Cpf(cpf);
        var query = _contexto.Pessoa.Where(p => p.Cpf == cpfVo);
        if (excluirId.HasValue)
        {
            query = query.Where(p => p.Id != excluirId.Value);
        }

        return await query.AnyAsync();
    }
}
