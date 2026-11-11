using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ConsultaAdendoRepositorio(ConsultorioContexto contexto) : Repositorio<ConsultaAdendo>(contexto), IConsultaAdendoRepositorio
{
    public override Task Remover(ConsultaAdendo entidade) =>
        throw new InvalidOperationException("Adendos de prontuário não podem ser removidos.");

    public async Task<List<ConsultaAdendo>> ObterPorConsulta(int idConsulta)
    {
        return await _contexto.ConsultaAdendo
            .Where(a => a.IdConsulta == idConsulta)
            .OrderBy(a => a.DtCriacao)
            .AsNoTracking()
            .ToListAsync();
    }
}
