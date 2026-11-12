using Consultorio.Server.Extensoes;
using Consultorio.Server.IoC;
using Consultorio.Server.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigurarRespostaValidacao();
builder.Services.AddOpenApi();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.ConfigAuthentication(builder.Configuration);
builder.Services.AdicionarPoliticasAcesso();
builder.Services.AdicionarLimitePortal();
builder.Services.AddHostedService<Consultorio.Server.Rotinas.RotinasAgendaHostedService>();

var app = builder.Build();

app.Services.Migrations(app.Configuration);

app.UseMiddleware<TratamentoErrosMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
