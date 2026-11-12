using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ConvenioRepositorio(ConsultorioContexto contexto) : Repositorio<Convenio>(contexto), IConvenioRepositorio
{
    public override Task Remover(Convenio entidade) =>
        throw new InvalidOperationException("Convênios não são removidos; use a inativação.");

    public Task<bool> ExisteNome(string nome, int? excluirId = null)
    {
        var alvo = (nome ?? string.Empty).Trim().ToLower();
        return _contexto.Convenio.AnyAsync(c => c.Nome.ToLower() == alvo && (excluirId == null || c.Id != excluirId));
    }
}
