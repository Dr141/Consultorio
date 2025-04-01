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
        var jwtOptionsSecurityKey = configuration.GetSection("JwtOptions:SecurityKey").Value;
        if(string.IsNullOrEmpty(jwtOptionsSecurityKey)) throw new ArgumentNullException("SecurityKey não configurada");

        services.Configure<JwtOptions>(configuration.GetSection("JwtOptions"));        
        services.Configure<IdentityOptions>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 6;
        });

        TokenValidationParameters tokenValidationParameters = new ()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = configuration.GetSection("JwtOptions:Issuer").Value,
            ValidAudience = configuration.GetSection("JwtOptions:Audience").Value,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptionsSecurityKey))
        };

        JwtBearerEvents jwtBearerEvents = new ()
        {
            OnMessageReceived = context =>
            {
                context.Token = "Refresh".Equals(context.Request.RouteValues["action"]) 
                                         ? context.Request.Cookies["RefreshToken"] 
                                         : context.Request.Cookies["AccessToken"];
                return Task.CompletedTask;
            }
            //,
            //OnForbidden = context =>
            //{
            //    return Task.CompletedTask;
            //},
            //OnAuthenticationFailed = context =>
            //{
            //    Console.WriteLine("Log do erro de autenticação");
            //    Console.WriteLine($"Erro de Autenticação: {context.Exception.Message}");
            //    if (context.Exception.InnerException != null)
            //        Console.WriteLine($"Erro de Autenticação InnerException: {context.Exception.InnerException.Message}");
            //    return Task.CompletedTask;
            //},
            //OnTokenValidated = context =>
            //{
            //    Console.WriteLine("Log do token validado");
            //    Console.WriteLine("Token Validado!");
            //    return Task.CompletedTask;
            //}
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
