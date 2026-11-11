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
    public DbSet<ConsultaAdendo> ConsultaAdendo { get; set; } = null!;
    public DbSet<RegistroAuditoria> RegistroAuditoria { get; set; } = null!;
    public DbSet<BloqueioAgenda> BloqueioAgenda { get; set; } = null!;

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
            builder.Property(p => p.Email).HasMaxLength(256);
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

            builder.Property(p => p.Ativo).IsRequired();
            builder.Property(p => p.DtInativacao);
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

            builder.Property(m => m.Ativo).IsRequired();
            builder.Property(m => m.DtInativacao);
            builder.Property(m => m.EmailUsuario).HasMaxLength(256);
            builder.HasIndex(m => m.EmailUsuario);
        });

        // Configuração de Agenda
        modelBuilder.Entity<Agenda>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Data).IsRequired();
            builder.Property(a => a.Hora).IsRequired();
            builder.Ignore(a => a.Disponivel);
            builder.Ignore(a => a.InicioLocal);
            builder.Property(a => a.Tipo).HasConversion<int>().IsRequired();
            builder.Property(a => a.DuracaoMinutos).IsRequired();
            builder.Property(a => a.Status).HasConversion<int?>();
            builder.Property(a => a.TokenConfirmacao);
            builder.HasIndex(a => a.TokenConfirmacao).IsUnique();
            builder.HasIndex(a => new { a.Data, a.IdMedico });

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
            builder.Property(c => c.Status).HasConversion<int>().IsRequired();
            builder.Property(c => c.DtFinalizacao);
            builder.Property(c => c.DtAnulacao);
            builder.Property(c => c.MotivoAnulacao).HasMaxLength(500);

            builder.HasMany(c => c.Adendos)
                .WithOne()
                .HasForeignKey(a => a.IdConsulta)
                .OnDelete(DeleteBehavior.Cascade);
            builder.Navigation(c => c.Adendos).UsePropertyAccessMode(PropertyAccessMode.Field);

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
            builder.Property(e => e.Cancelado).IsRequired();
            builder.Property(e => e.DtCancelamento);
            builder.Property(e => e.MotivoCancelamento).HasMaxLength(500);

            builder.HasOne(e => e.Paciente)
                .WithMany(p => p.Exames)
                .HasForeignKey(e => e.IdPaciente)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            builder.HasOne(e => e.Agenda)
                .WithOne(a => a.Exame)
                .HasForeignKey<Exame>(e => e.IdAgenda)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasOne(e => e.Medico)
                .WithMany(m => m.Exames)
                .HasForeignKey(e => e.IdMedico)
                .OnDelete(DeleteBehavior.SetNull);
        });
        // Adendos de consulta (complementos após finalização)
        modelBuilder.Entity<ConsultaAdendo>(builder =>
        {
            builder.ToTable("ConsultaAdendo");
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Texto).HasMaxLength(ConsultaAdendo.TamanhoMaximoTexto).IsRequired();
            builder.Property(a => a.Autor).HasMaxLength(256).IsRequired();
            builder.HasIndex(a => a.IdConsulta);
        });

        // Trilha de auditoria (LGPD)
        modelBuilder.Entity<RegistroAuditoria>(builder =>
        {
            builder.ToTable("RegistroAuditoria");
            builder.HasKey(r => r.Id);
            builder.Property(r => r.Usuario).HasMaxLength(256).IsRequired();
            builder.Property(r => r.Acao).HasMaxLength(30).IsRequired();
            builder.Property(r => r.Recurso).HasMaxLength(60).IsRequired();
            builder.Property(r => r.IdRecurso).HasMaxLength(60);
            builder.Property(r => r.Metodo).HasMaxLength(10).IsRequired();
            builder.Property(r => r.Caminho).HasMaxLength(400).IsRequired();
            builder.Property(r => r.Ip).HasMaxLength(64);
            builder.HasIndex(r => r.DtCriacao);
            builder.HasIndex(r => new { r.Recurso, r.IdRecurso });
        });
        // Bloqueios de agenda (férias, feriados, congressos)
        modelBuilder.Entity<BloqueioAgenda>(builder =>
        {
            builder.ToTable("BloqueioAgenda");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.DataInicio).IsRequired();
            builder.Property(b => b.DataFim).IsRequired();
            builder.Property(b => b.Motivo).HasMaxLength(200).IsRequired();
            builder.HasOne(b => b.Medico)
                .WithMany()
                .HasForeignKey(b => b.IdMedico)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasIndex(b => new { b.DataInicio, b.DataFim });
        });
    }
}
