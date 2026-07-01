namespace QueenZone.NewsAgent;

public sealed record DiscoverNewsCommandOptions(
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
                    return null;
            }
        }

        return new DiscoverNewsCommandOptions(seedSources, dryRun, force);
    }
}
