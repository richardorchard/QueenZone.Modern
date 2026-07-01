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
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("QueenZone.NewsAgent.Worker");
var discoveryService = provider.GetRequiredService<NewsDiscoveryService>();
var triageService = provider.GetRequiredService<NewsTriageService>();
var aiRunExecutor = provider.GetRequiredService<NewsAiRunExecutor>();
var openRouterOptions = provider.GetRequiredService<IOptions<OpenRouterOptions>>().Value;

if (!aiRunExecutor.IsAiEnabled)
{
    logger.LogWarning("OpenRouter AI processing is disabled. Fetch-only discovery will continue without AI triage or drafting.");
}
else if (openRouterOptions.DryRun)
{
    logger.LogInformation("OpenRouter dry-run mode is enabled. AI requests will be logged without calling the provider.");
}

var exitCode = 0;

if (!options.TriageOnly)
{
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

    if (result.Failures > 0)
    {
        exitCode = 1;
    }
}

if (options.Triage)
{
    var triageResult = await triageService.RunTriageAsync(new NewsTriageRunOptions(DryRun: options.DryRun));

    logger.LogInformation(
        "Triage finished. Considered={CandidatesConsidered}, promoted={PromotedToReview}, rejected={Rejected}, duplicates={MarkedDuplicate}, skipped={Skipped}, failures={Failures}.",
        triageResult.CandidatesConsidered,
        triageResult.PromotedToReview,
        triageResult.Rejected,
        triageResult.MarkedDuplicate,
        triageResult.Skipped,
        triageResult.Failures);

    foreach (var error in triageResult.Errors)
    {
        logger.LogError("Triage error: {Error}", error);
    }

    if (triageResult.Failures > 0)
    {
        exitCode = 1;
    }
}

return exitCode;

static void PrintUsage()
{
    Console.WriteLine("QueenZone news discovery worker");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/QueenZone.NewsAgent.Worker -- discover-news [--fetch-only] [--seed-sources] [--dry-run] [--force] [--triage] [--triage-only]");
}
