using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class AcessoPortalRepositorio(ConsultorioContexto contexto) : Repositorio<AcessoPortal>(contexto), IAcessoPortalRepositorio
{
    public Task<AcessoPortal?> ObterPorChave(Guid chave) =>
        _contexto.AcessoPortal.FirstOrDefaultAsync(a => a.Chave == chave);

    public Task<AcessoPortal?> ObterPorTokenHash(string tokenHash) =>
        _contexto.AcessoPortal
            .Include(a => a.Paciente).ThenInclude(p => p.Pessoa)
            .FirstOrDefaultAsync(a => a.TokenHash == tokenHash);

    public Task<int> ContarSolicitacoes(int idPaciente, DateTime desdeUtc)
    {
        var desde = DateTime.SpecifyKind(desdeUtc, DateTimeKind.Utc);
        return _contexto.AcessoPortal.CountAsync(a => a.IdPaciente == idPaciente && a.DtCriacao >= desde);
    }
}
