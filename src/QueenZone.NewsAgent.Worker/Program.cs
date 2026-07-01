using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QueenZone.Data;
using QueenZone.NewsAgent;

var options = DiscoverNewsCommandOptions.Parse(args);
if (options is null)
{
    PrintUsage();
    return 1;
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Local.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var services = new ServiceCollection();
services.AddLogging(builder => builder.AddConsole());
services.AddQueenZoneNewsAgent(configuration);

var connectionString = configuration.GetConnectionString("QueenZoneLegacy");
if (string.IsNullOrWhiteSpace(connectionString))
{
    services.AddQueenZoneInMemoryData();
}
else
{
    services.AddQueenZoneLegacyData(connectionString);
}

await using var provider = services.BuildServiceProvider();
var worker = provider.GetRequiredService<DiscoverNewsWorker>();

return await worker.RunAsync(options);

static void PrintUsage()
{
    Console.WriteLine("QueenZone news discovery worker");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news [--fetch-only] [--seed-sources] [--dry-run] [--force] [--triage] [--triage-only] [--draft] [--draft-only]");
}
