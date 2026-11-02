using Consultorio.Modelo.Entidades;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Contexto;

/// <summary>
/// Contexto do banco de dados
/// </summary>
public class ConsultorioContexto(DbContextOptions<ConsultorioContexto> options) : DbContext(options)
{
    #region DbSets
    public DbSet<Consulta> Consulta { get; set; }
    public DbSet<Exame> Exame { get; set; }
    public DbSet<Paciente> Paciente { get; set; }
    public DbSet<Medico> Medico { get; set; }    
    public DbSet<Pessoa> Pessoa { get; set; }
    #endregion

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Medico>()
            .HasMany(c => c.Consultas)
            .WithOne(m => m.Medico)
            .HasForeignKey(c => c.IdMedico)
            .IsRequired();

        modelBuilder.Entity<Consulta>()
            .HasOne(c => c.Paciente)
            .WithMany(p => p.Consultas)
            .HasForeignKey(c => c.IdPaciente)
            .IsRequired();

        modelBuilder.Entity<Exame>()
            .HasOne(e => e.Paciente)
            .WithMany(p => p.Exames)
            .HasForeignKey(e => e.IdPaciente)
            .IsRequired();

        modelBuilder.Entity<Paciente>()
            .HasMany(p => p.Consultas)
            .WithOne(c => c.Paciente)
            .HasForeignKey(p => p.IdPaciente)
            .IsRequired();

        modelBuilder.Entity<Paciente>()
            .HasOne(p => p.Pessoa)
            .WithOne(e => e.Paciente)
            .HasForeignKey<Paciente>(p => p.IdPessoa)
            .IsRequired();

        modelBuilder.Entity<Medico>()
            .HasOne(m => m.Pessoa)
            .WithOne(e => e.Medico)
            .HasForeignKey<Medico>(m => m.IdPessoa)
            .IsRequired();

        modelBuilder.Entity<Agenda>()
            .HasOne(a => a.Medico)
            .WithMany(m => m.Agendas)
            .HasForeignKey(a => a.IdMedico)
            .IsRequired(false);
                
        modelBuilder.Entity<Agenda>()
            .HasOne(a => a.Paciente)
            .WithMany(p => p.Agendas)
            .HasForeignKey(a => a.IdPaciente)
            .IsRequired(false);

        modelBuilder.Entity<Exame>()
            .HasOne(e => e.Agenda)
            .WithOne(a => a.Exame)
            .HasForeignKey<Exame>(e => e.IdAgenda)
            .IsRequired(false);

        modelBuilder.Entity<Consulta>()
            .HasOne(c => c.Agenda)
            .WithOne(a => a.Consulta)
            .HasForeignKey<Consulta>(c => c.IdAgenda)
            .IsRequired(false);
    }
}
