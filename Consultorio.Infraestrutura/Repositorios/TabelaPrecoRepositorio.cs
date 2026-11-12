using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class TabelaPrecoRepositorio(ConsultorioContexto contexto) : Repositorio<TabelaPreco>(contexto), ITabelaPrecoRepositorio
{
    public Task<TabelaPreco?> ObterPor(TipoAtendimento tipo, int? idConvenio) =>
        _contexto.TabelaPreco.FirstOrDefaultAsync(t => t.Tipo == tipo && t.IdConvenio == idConvenio);

    public Task<List<TabelaPreco>> ObterTodosDetalhado() =>
        _contexto.TabelaPreco
            .Include(t => t.Convenio)
            .AsNoTracking()
            .OrderBy(t => t.IdConvenio).ThenBy(t => t.Tipo)
            .ToListAsync();
}
