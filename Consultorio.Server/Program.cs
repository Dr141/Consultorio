using Consultorio.Server.Extensoes;
using Consultorio.Server.IoC;
using Consultorio.Server.Middlewares;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigurarRespostaValidacao();
builder.Services.AddOpenApi();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.ConfigAuthentication(builder.Configuration);

var app = builder.Build();

app.Services.Migrations(app.Configuration);

app.UseMiddleware<TratamentoErrosMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapStaticAssets();
app.MapOpenApi();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
