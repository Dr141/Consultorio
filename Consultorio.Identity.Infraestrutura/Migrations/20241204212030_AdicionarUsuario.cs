using Consultorio.Identity.Infraestrutura.Contexto;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

#nullable disable

namespace Consultorio.Identity.Infraestrutura.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var adminRoleId = Guid.NewGuid().ToString();
            var adminUserId = Guid.NewGuid().ToString();
            var services = new ServiceCollection();

            // Add Identity services
            services.AddIdentityCore<IdentityUser>()
                    .AddRoles<IdentityRole>()
                    .AddEntityFrameworkStores<IdentityContexto>()
                    .AddDefaultTokenProviders();
            var serviceProvider = services.BuildServiceProvider();

            var passwordHasher = serviceProvider.GetRequiredService<IPasswordHasher<IdentityUser>>();
            var user = new IdentityUser { UserName = "admin@domain.com", Email = "admin@domain.com" };
            string passwordHash = passwordHasher.HashPassword(user, "Admin@123");

            migrationBuilder.Sql($@"
            INSERT INTO public.""AspNetRoles"" (id, name, normalized_name)
            VALUES ('{adminRoleId}', 'Administrador', 'ADMINISTRADOR');");

            migrationBuilder.Sql($@"INSERT INTO public.""AspNetUsers"" (id, user_name, normalized_user_name, email, normalized_email, email_confirmed, password_hash, security_stamp, concurrency_stamp, phone_number_confirmed, two_factor_enabled, lockout_enabled, access_failed_count)
            VALUES ('{adminUserId}', 'admin@domain.com', 'ADMIN@DOMAIN.COM', 'admin@domain.com', 'ADMIN@DOMAIN.COM', true, '{passwordHash}', '{Guid.NewGuid().ToString()}', '{Guid.NewGuid().ToString()}', false, false, true, 0);");

            migrationBuilder.Sql($@"INSERT INTO public.""AspNetUserRoles"" (user_id, role_id)
            VALUES ('{adminUserId}', '{adminRoleId}');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM public.\"AspNetUserRoles\" WHERE user_id IN (SELECT Id FROM AspNetUsers WHERE user_name = 'admin')");
            migrationBuilder.Sql("DELETE FROM public.\"AspNetUsers\" WHERE user_name = 'admin'");
            migrationBuilder.Sql("DELETE FROM public.\"AspNetRoles\" WHERE Name = 'Administrador'");
        }
    }
}
