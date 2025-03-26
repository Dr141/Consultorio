using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Consultorio.Identity.Infraestrutura.Contexto;

public class IdentityContexto : IdentityDbContext
{
    /// <summary>
    /// Construtor da classe <see cref="IdentityContexto"/>
    /// </summary>
    /// <param name="options"></param>
    public IdentityContexto(DbContextOptions<IdentityContexto> options) : base(options) { }

    /// <summary>
    /// Configuração para padronizar os nomes case em base de dados PostgreSql
    /// </summary>
    /// <param name="optionsBuilder"></param>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    => optionsBuilder.UseSnakeCaseNamingConvention();
}
