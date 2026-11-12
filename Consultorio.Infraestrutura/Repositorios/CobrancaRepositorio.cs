using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class CobrancaRepositorio(ConsultorioContexto contexto) : Repositorio<Cobranca>(contexto), ICobrancaRepositorio
{
    public override Task Remover(Cobranca entidade) =>
        throw new InvalidOperationException("Cobranças não são removidas; use o cancelamento ou o estorno.");

    private IQueryable<Cobranca> Detalhado() =>
        _contexto.Cobranca
            .Include(c => c.Paciente).ThenInclude(p => p.Pessoa)
            .Include(c => c.Convenio);

    public Task<Cobranca?> ObterDetalhadoPorId(int id) =>
        Detalhado().FirstOrDefaultAsync(c => c.Id == id);

    public async Task<List<Cobranca>> ObterPorPeriodo(DateTime inicio, DateTime fim, StatusCobranca? status = null, int? idPaciente = null)
    {
        // Folga de um dia em cada ponta por causa do fuso; o serviço refina pela data local.
        var ini = DateTime.SpecifyKind(inicio.Date.AddDays(-1), DateTimeKind.Utc);
        var f = DateTime.SpecifyKind(fim.Date.AddDays(2), DateTimeKind.Utc);
        var query = Detalhado().AsNoTracking().Where(c => c.DtCriacao >= ini && c.DtCriacao < f);
        if (status.HasValue) query = query.Where(c => c.Status == status.Value);
        if (idPaciente is > 0) query = query.Where(c => c.IdPaciente == idPaciente.Value);
        return await query.OrderByDescending(c => c.DtCriacao).ToListAsync();
    }

    public Task<List<Cobranca>> ObterPagasNoPeriodo(DateTime inicioUtc, DateTime fimUtc)
    {
        var ini = DateTime.SpecifyKind(inicioUtc, DateTimeKind.Utc);
        var f = DateTime.SpecifyKind(fimUtc, DateTimeKind.Utc);
        return Detalhado().AsNoTracking()
            .Where(c => c.DtPagamento != null && c.DtPagamento >= ini && c.DtPagamento < f
                        && (c.Status == StatusCobranca.Paga || c.Status == StatusCobranca.Estornada))
            .ToListAsync();
    }

    public Task<List<Cobranca>> ObterPendentes() =>
        Detalhado().AsNoTracking()
            .Where(c => c.Status == StatusCobranca.Pendente)
            .OrderBy(c => c.DtVencimento)
            .ToListAsync();

    public Task<List<Cobranca>> ObterPorPaciente(int idPaciente) =>
        Detalhado().AsNoTracking()
            .Where(c => c.IdPaciente == idPaciente)
            .OrderByDescending(c => c.DtCriacao)
            .ToListAsync();

    public async Task<HashSet<int>> AgendasComCobranca(IEnumerable<int> idsAgenda)
    {
        var ids = idsAgenda.Distinct().ToList();
        if (ids.Count == 0) return new HashSet<int>();
        var encontrados = await _contexto.Cobranca
            .Where(c => c.IdAgenda != null && ids.Contains(c.IdAgenda.Value) && c.Status != StatusCobranca.Cancelada)
            .Select(c => c.IdAgenda!.Value)
            .ToListAsync();
        return encontrados.ToHashSet();
    }
}
