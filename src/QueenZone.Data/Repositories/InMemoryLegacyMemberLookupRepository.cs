namespace QueenZone.Data;

public sealed class InMemoryLegacyMemberLookupRepository : ILegacyMemberLookupRepository
{
    private readonly Dictionary<string, List<LegacyMemberMatch>> matchesByEmail;

    /// <summary>
    /// Single-match convenience map (email → one legacy row). Prefer the multi-match constructor
    /// when an email can resolve to more than one USERS_T account.
    /// </summary>
    public InMemoryLegacyMemberLookupRepository(IReadOnlyDictionary<string, LegacyMemberMatch> matchesByEmail)
        : this(matchesByEmail.ToDictionary(
            static pair => pair.Key,
            static pair => (IReadOnlyList<LegacyMemberMatch>)[pair.Value],
            StringComparer.OrdinalIgnoreCase))
    {
    }

    public InMemoryLegacyMemberLookupRepository(
        IReadOnlyDictionary<string, IReadOnlyList<LegacyMemberMatch>> matchesByEmail)
    {
        this.matchesByEmail = new Dictionary<string, List<LegacyMemberMatch>>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in matchesByEmail)
        {
            this.matchesByEmail[pair.Key] = pair.Value
                .OrderBy(match => match.Username, StringComparer.OrdinalIgnoreCase)
                .ThenBy(match => match.UserId)
                .ToList();
        }
    }

    public Task<IReadOnlyList<LegacyMemberMatch>> FindAllByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (matchesByEmail.TryGetValue(email, out var matches))
        {
            return Task.FromResult<IReadOnlyList<LegacyMemberMatch>>(matches);
        }

        return Task.FromResult<IReadOnlyList<LegacyMemberMatch>>([]);
    }

    public async Task<LegacyMemberMatch?> FindByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var matches = await FindAllByEmailAsync(email, cancellationToken);
        return matches.FirstOrDefault();
    }

    public Task<LegacyMemberMatch?> FindByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var match = matchesByEmail.Values
            .SelectMany(static items => items)
            .FirstOrDefault(item => item.UserId == userId);
        return Task.FromResult(match);
    }
}
