namespace QueenZone.Data;

public sealed class InMemoryLegacyMemberLookupRepository : ILegacyMemberLookupRepository
{
    private readonly IReadOnlyDictionary<string, LegacyMemberMatch> matchesByEmail;

    public InMemoryLegacyMemberLookupRepository(IReadOnlyDictionary<string, LegacyMemberMatch> matchesByEmail)
    {
        this.matchesByEmail = new Dictionary<string, LegacyMemberMatch>(matchesByEmail, StringComparer.OrdinalIgnoreCase);
    }

    public Task<LegacyMemberMatch?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(matchesByEmail.TryGetValue(email, out var match) ? match : null);
}
