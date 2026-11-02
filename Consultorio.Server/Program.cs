using Consultorio.Server.Extensoes;
using Consultorio.Server.IoC;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.ConfigAuthentication(builder.Configuration);

var app = builder.Build();
app.Services.Migrations(app.Configuration);
app.UseDefaultFiles();
app.MapStaticAssets();
app.MapOpenApi();
app.MapControllers();
app.UseAuthorization();
app.MapFallbackToFile("index.html");
app.Run();
