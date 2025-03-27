using Consultorio.Modelo.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Contexto;

/// <summary>
/// Contexto do banco de dados
/// </summary>
public class ConsultorioContexto : DbContext
{
    #region DbSets
    public DbSet<Consulta> Consulta { get; set; }
    public DbSet<Exame> Exame { get; set; }
    public DbSet<Paciente> Paciente { get; set; }
    public DbSet<Medico> Medico { get; set; }    
    public DbSet<Pessoa> Pessoa { get; set; }
    #endregion

    public ConsultorioContexto(DbContextOptions<ConsultorioContexto> options) : base(options) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseNpgsql();
}
