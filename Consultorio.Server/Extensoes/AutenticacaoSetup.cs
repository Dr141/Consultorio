using Consultorio.Identity.Modelo.Configuracao;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Consultorio.Server.Extensoes;

public static class AutenticacaoSetup
{
    public static void ConfigAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtAppSettingOptions = configuration.GetSection(nameof(JwtOptions));
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration.GetSection("JwtOptions:SecurityKey").Value));

        services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));
        
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
        });

        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration.GetSection("JwtOptions:Issuer").Value,
            ValidAudience = configuration.GetSection("JwtOptions:Audience").Value,
            IssuerSigningKey = securityKey
        };

        var jwtBearerEvents = new JwtBearerEvents
        {
#if DEBUG
            OnMessageReceived = context =>
            {                
                Console.WriteLine("Verifica se o token está presente no cabeçalho");
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                Console.WriteLine($"Headers['Authorization']: {token}");
                Console.WriteLine("Log do token recebido");
                Console.WriteLine($"Token: {context.Token}");
                Console.WriteLine();
                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine("Log do erro de autenticação");
                Console.WriteLine($"Erro de Autenticação: {context.Exception.Message}");
                if (context.Exception.InnerException != null)
                    Console.WriteLine($"Erro de Autenticação InnerException: {context.Exception.InnerException.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Log do token validado");
                Console.WriteLine("Token Validado!");
                return Task.CompletedTask;
            }
#endif
        };

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = tokenValidationParameters;
            options.Events = jwtBearerEvents;
        });
    }
}
