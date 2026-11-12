using Consultorio.Dominio.Entidades;

namespace Consultorio.Dominio.Interfaces.Repositorios;

public interface ICertificadoDigitalRepositorio : IRepositorio<CertificadoDigital>
{
    Task<CertificadoDigital?> ObterAtivoPorMedico(int idMedico);
}
