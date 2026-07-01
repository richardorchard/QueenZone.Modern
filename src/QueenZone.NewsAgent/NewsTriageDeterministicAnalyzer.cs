using QueenZone.Data;

namespace QueenZone.NewsAgent;

public sealed class NewsTriageDeterministicAnalyzer(INewsDiscoveryRepository repository)
{
    public async Task<NewsTriageDeterministicSignals> AnalyzeAsync(
        NewsCandidate candidate,
        NewsDiscoverySource source,
        IReadOnlyList<NewsCandidateEvidence> evidence,
        CancellationToken cancellationToken = default)
    {
        var duplicate = await repository.FindEarlierDuplicateCandidateAsync(
            candidate.Id,
            candidate.SourceTitle,
            candidate.ContentHash,
            cancellationToken);

        var keywordMatch = MatchesSourceKeywords(source, candidate, evidence);
        var canonicalUrlKnown = !string.IsNullOrWhiteSpace(candidate.CanonicalUrl);

        return new NewsTriageDeterministicSignals(
            keywordMatch,
            canonicalUrlKnown,
            duplicate?.Id,
            duplicate is null
                ? null
                : $"Earlier candidate {duplicate.Id} matches title or content hash.");
    }

    private static bool MatchesSourceKeywords(
        NewsDiscoverySource source,
        NewsCandidate candidate,
        IReadOnlyList<NewsCandidateEvidence> evidence)
    {
        var excerpt = evidence.Count == 0
            ? null
            : string.Join(' ', evidence.Select(item => item.Excerpt).Where(value => !string.IsNullOrWhiteSpace(value)));

        return NewsDiscoveryKeywordFilter.Matches(
            source,
            new FetchedNewsItem(candidate.SourceUrl, candidate.SourceTitle, candidate.SourcePublishedAt, excerpt));
    }
}
