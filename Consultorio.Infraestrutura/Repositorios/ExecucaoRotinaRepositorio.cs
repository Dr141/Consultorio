using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Interfaces.Repositorios;
using Consultorio.Infraestrutura.Contexto;
using Consultorio.Infraestrutura.Repositorios.Base;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Repositorios;

public class ExecucaoRotinaRepositorio(ConsultorioContexto contexto) : Repositorio<ExecucaoRotina>(contexto), IExecucaoRotinaRepositorio
{
    public Task<ExecucaoRotina?> ObterPorNome(string nome) =>
        _contexto.ExecucaoRotina.FirstOrDefaultAsync(e => e.Nome == nome);
}
