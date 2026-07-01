namespace QueenZone.NewsAgent;

public sealed record DiscoverNewsCommandOptions(
    bool SeedSources,
    bool DryRun,
    bool Force,
    bool Triage,
    bool TriageOnly,
    bool Draft,
    bool DraftOnly)
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
        var triage = false;
        var triageOnly = false;
        var draft = false;
        var draftOnly = false;
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
                case "--triage":
                    triage = true;
                    break;
                case "--triage-only":
                    triage = true;
                    triageOnly = true;
                    break;
                case "--draft":
                    draft = true;
                    break;
                case "--draft-only":
                    draft = true;
                    draftOnly = true;
                    break;
                case "--scheduled":
                    seedSources = true;
                    triage = true;
                    draft = true;
                    break;
                default:
                    return null;
            }
        }

        return new DiscoverNewsCommandOptions(seedSources, dryRun, force, triage, triageOnly, draft, draftOnly);
    }
}
