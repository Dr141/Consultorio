using Consultorio.Dominio.Entidades;
using Consultorio.Dominio.Enumerados;
using Consultorio.Dominio.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Infraestrutura.Contexto;

public class ConsultorioContexto(DbContextOptions<ConsultorioContexto> options) : DbContext(options)
{
    public DbSet<Pessoa> Pessoa { get; set; } = null!;
    public DbSet<Paciente> Paciente { get; set; } = null!;
    public DbSet<Medico> Medico { get; set; } = null!;
    public DbSet<Agenda> Agenda { get; set; } = null!;
    public DbSet<Consulta> Consulta { get; set; } = null!;
    public DbSet<Exame> Exame { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuração de Pessoa
        modelBuilder.Entity<Pessoa>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Nome).HasMaxLength(100).IsRequired();
            builder.Property(p => p.Cpf)
                .HasConversion(cpf => cpf.Valor, valor => new Cpf(valor))
                .HasMaxLength(11)
                .IsRequired();
            builder.HasIndex(p => p.Cpf).IsUnique();
            builder.Property(p => p.Telefone)
                .HasConversion(tel => tel.Valor, valor => new Telefone(valor))
                .HasMaxLength(11)
                .IsRequired();
            builder.Property(p => p.Observacao).HasMaxLength(500);
            builder.Property(p => p.DtNasc).IsRequired();
        });

        // Configuração de Paciente
        modelBuilder.Entity<Paciente>(builder =>
        {
            builder.HasKey(p => p.Id);
            builder.HasOne(p => p.Pessoa)
                .WithOne(e => e.Paciente)
                .HasForeignKey<Paciente>(p => p.IdPessoa)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // Configuração de Medico
        modelBuilder.Entity<Medico>(builder =>
        {
            builder.HasKey(m => m.Id);
            builder.Property(m => m.Crm)
                .HasConversion(crm => crm.Valor, valor => new Crm(valor))
                .HasMaxLength(20)
                .IsRequired();
            builder.HasIndex(m => m.Crm).IsUnique();

            builder.Property(m => m.Especialidades)
                .HasConversion(
                    esp => esp.Select(e => (int)e).ToArray(),
                    arr => (IReadOnlyCollection<Especialidade>)(arr == null
                        ? new List<Especialidade>()
                        : arr.Cast<Especialidade>().ToList())
                )
                .HasColumnType("integer[]")
                .IsRequired();

            builder.HasOne(m => m.Pessoa)
                .WithOne(e => e.Medico)
                .HasForeignKey<Medico>(m => m.IdPessoa)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        // Configuração de Agenda
        modelBuilder.Entity<Agenda>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Data).IsRequired();
            builder.Property(a => a.Hora).IsRequired();
            builder.Ignore(a => a.Disponivel);

            builder.HasOne(a => a.Medico)
                .WithMany(m => m.Agendas)
                .HasForeignKey(a => a.IdMedico)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(a => a.Paciente)
                .WithMany(p => p.Agendas)
                .HasForeignKey(a => a.IdPaciente)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuração de Consulta
        modelBuilder.Entity<Consulta>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Observacao).HasMaxLength(2000).IsRequired();

            builder.HasOne(c => c.Paciente)
                .WithMany(p => p.Consultas)
                .HasForeignKey(c => c.IdPaciente)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(c => c.Medico)
                .WithMany(m => m.Consultas)
                .HasForeignKey(c => c.IdMedico)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(c => c.Agenda)
                .WithOne(a => a.Consulta)
                .HasForeignKey<Consulta>(c => c.IdAgenda)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configuração de Exame
        modelBuilder.Entity<Exame>(builder =>
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Descricao).HasMaxLength(200).IsRequired();
            builder.Property(e => e.Resultado);

            builder.HasOne(e => e.Paciente)
                .WithMany(p => p.Exames)
                .HasForeignKey(e => e.IdPaciente)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(e => e.Agenda)
                .WithOne(a => a.Exame)
                .HasForeignKey<Exame>(e => e.IdAgenda)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
