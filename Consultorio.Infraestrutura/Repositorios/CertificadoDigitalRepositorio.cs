using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class CertificadoDigitalRepositorio(ConsultorioContexto contexto) : Repositorio<CertificadoDigital>(contexto), ICertificadoDigitalRepositorio
{
    public Task<CertificadoDigital?> ObterAtivoPorMedico(int idMedico) =>
        _contexto.CertificadoDigital
            .Where(c => c.IdMedico == idMedico && c.Ativo)
            .OrderByDescending(c => c.DtCriacao)
            .FirstOrDefaultAsync();
}
