using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class DocumentoAssinaturaRepositorio(ConsultorioContexto contexto) : Repositorio<DocumentoAssinatura>(contexto), IDocumentoAssinaturaRepositorio
{
    public override Task Remover(DocumentoAssinatura entidade) =>
        throw new InvalidOperationException("Assinaturas digitais fazem parte do prontuário e não podem ser removidas.");

    public Task<DocumentoAssinatura?> ObterPorDocumento(int idDocumento) =>
        _contexto.DocumentoAssinatura.AsNoTracking().FirstOrDefaultAsync(a => a.IdDocumento == idDocumento);
}
