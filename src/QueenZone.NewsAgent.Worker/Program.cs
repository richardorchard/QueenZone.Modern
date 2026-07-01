using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
services.AddQueenZoneNewsAgent();

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
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("QueenZone.NewsAgent.Worker");
var discoveryService = provider.GetRequiredService<NewsDiscoveryService>();

var runOptions = new NewsDiscoveryRunOptions(
    SeedSources: options.SeedSources,
    FetchOnly: true,
    DryRun: options.DryRun,
    Force: options.Force);

var result = await discoveryService.RunFetchAsync(runOptions);

logger.LogInformation(
    "Discovery finished. Sources checked={SourcesChecked}, skipped={SourcesSkipped}, items={ItemsFetched}, created={CandidatesCreated}, duplicates={DuplicatesSkipped}, keyword filtered={KeywordFiltered}, failures={Failures}.",
    result.SourcesChecked,
    result.SourcesSkipped,
    result.ItemsFetched,
    result.CandidatesCreated,
    result.DuplicatesSkipped,
    result.KeywordFiltered,
    result.Failures);

foreach (var error in result.Errors)
{
    logger.LogError("Discovery error: {Error}", error);
}

return result.Failures > 0 ? 1 : 0;

static void PrintUsage()
{
    Console.WriteLine("QueenZone news discovery worker");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news [--fetch-only] [--seed-sources] [--dry-run] [--force]");
}

internal sealed record DiscoverNewsCommandOptions(
    bool SeedSources,
    bool DryRun,
    bool Force)
{
    public static DiscoverNewsCommandOptions? Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "discover-news", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var seedSources = false;
        var dryRun = false;
        var force = false;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index].ToLowerInvariant())
            {
                case "--fetch-only":
                    break;
                case "--seed-sources":
                    seedSources = true;
                    break;
                case "--dry-run":
                    dryRun = true;
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown argument: {args[index]}");
                    return null;
            }
        }

        return new DiscoverNewsCommandOptions(seedSources, dryRun, force);
    }
}
