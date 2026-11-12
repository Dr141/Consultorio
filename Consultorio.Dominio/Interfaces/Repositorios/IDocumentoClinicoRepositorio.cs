using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface IDocumentoClinicoRepositorio : IRepositorio<DocumentoClinico>
{
    Task<DocumentoClinico?> ObterDetalhadoPorId(int id);
    Task<DocumentoClinico?> ObterPorCodigo(string codigoVerificacao);
    Task<List<DocumentoClinico>> ObterPorPaciente(int idPaciente);
}
