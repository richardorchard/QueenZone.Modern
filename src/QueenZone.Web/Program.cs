using QueenZone.Data;
using QueenZone.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
}

var legacyConnectionString = builder.Configuration.GetConnectionString("QueenZoneLegacy");

builder.Services.AddSingleton<INewsRepository>(_ =>
    string.IsNullOrWhiteSpace(legacyConnectionString)
        ? new SampleNewsRepository()
        : new LegacyNewsRepository(legacyConnectionString));

var app = builder.Build();

app.MapNewsRoutes();

app.Run();

public partial class Program;
