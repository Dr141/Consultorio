using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Consultorio.Identity.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AddRolesAndClaims : Migration
    {
        private readonly Dictionary<string, string> _roles = new Dictionary<string, string>()
        {
            { "Administrativo", "ADMINISTRATIVO" },
            { "Auxiliar", "AUXILIAR" },
            { "Medico", "MEDICO" }
        };
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var role in _roles) 
            {
                migrationBuilder.Sql($@"
                INSERT INTO public.""AspNetRoles"" (id, name, normalized_name)
                VALUES ('{Guid.NewGuid().ToString()}', '{role.Key}', '{role.Value}');");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            foreach (var role in _roles)
            {
                migrationBuilder.Sql($"DELETE FROM public.\"AspNetRoles\" WHERE Name = '{role.Key}'");
            }
        }
    }
}
