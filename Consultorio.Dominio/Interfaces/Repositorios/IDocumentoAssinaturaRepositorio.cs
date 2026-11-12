using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IDocumentoAssinaturaRepositorio : IRepositorio<DocumentoAssinatura>
{
    Task<DocumentoAssinatura?> ObterPorDocumento(int idDocumento);
}
