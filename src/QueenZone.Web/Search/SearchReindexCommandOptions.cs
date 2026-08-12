namespace QueenZone.Web.Search;

public sealed record SearchReindexCommandOptions(bool Force)
{
    public static SearchReindexCommandOptions? Parse(string[] args)
    {
        if (args.Length == 0 || !string.Equals(args[0], "reindex", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var force = false;
        for (var index = 1; index < args.Length; index++)
        {
            switch (args[index].ToLowerInvariant())
            {
                case "--scheduled":
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    return null;
            }
        }

        return new SearchReindexCommandOptions(force);
    }
}
