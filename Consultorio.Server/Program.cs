using Consultorio.Server.Extensoes;
using Consultorio.Server.IoC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.RegisterServices(builder.Configuration);
builder.Services.ConfigAuthentication(builder.Configuration);
var app = builder.Build();
app.Services.Migrations();
app.UseDefaultFiles();
app.MapStaticAssets();

app.MapOpenApi();
//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();
