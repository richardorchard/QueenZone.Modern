namespace QueenZone.Data;

public static class SampleLegacyMemberData
{
    public static IReadOnlyDictionary<string, LegacyMemberMatch> CreateSeedMatches() => new Dictionary<string, LegacyMemberMatch>(StringComparer.OrdinalIgnoreCase)
    {
        ["legacy.fan@queenzone.org"] = new LegacyMemberMatch(35418, "Mike Ryde"),
    };
}
