using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class DocumentoClinicoRepositorio(ConsultorioContexto contexto) : Repositorio<DocumentoClinico>(contexto), IDocumentoClinicoRepositorio
{
    public override Task Remover(DocumentoClinico entidade) =>
        throw new InvalidOperationException("Documentos clínicos não podem ser removidos; use o cancelamento.");

    private IQueryable<DocumentoClinico> Detalhado() =>
        _contexto.DocumentoClinico
            .Include(d => d.Paciente).ThenInclude(p => p.Pessoa)
            .Include(d => d.Medico).ThenInclude(m => m.Pessoa);

    public Task<DocumentoClinico?> ObterDetalhadoPorId(int id) =>
        Detalhado().FirstOrDefaultAsync(d => d.Id == id);

    public Task<DocumentoClinico?> ObterPorCodigo(string codigoVerificacao)
    {
        var codigo = (codigoVerificacao ?? string.Empty).Trim().ToUpperInvariant();
        return Detalhado().AsNoTracking().FirstOrDefaultAsync(d => d.CodigoVerificacao == codigo);
    }

    public Task<List<DocumentoClinico>> ObterPorPaciente(int idPaciente) =>
        Detalhado().AsNoTracking()
            .Where(d => d.IdPaciente == idPaciente)
            .OrderByDescending(d => d.DtCriacao)
            .ToListAsync();
}
