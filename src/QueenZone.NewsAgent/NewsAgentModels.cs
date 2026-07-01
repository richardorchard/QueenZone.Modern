namespace QueenZone.NewsAgent;

public sealed record FetchedNewsItem(
    string SourceUrl,
    string Title,
    DateTime? PublishedAt,
    string? Excerpt);

public sealed record NewsDiscoveryRunOptions(
    bool SeedSources = false,
    bool FetchOnly = true,
    bool DryRun = false,
    bool Force = false,
    DateTime? RunAtUtc = null);

public sealed record NewsDiscoveryRunResult(
    int SourcesChecked,
    int SourcesSkipped,
    int ItemsFetched,
    int CandidatesCreated,
    int DuplicatesSkipped,
    int KeywordFiltered,
    int Failures,
    IReadOnlyList<string> Errors);

public sealed record NewsDiscoverySourceFetchLog(
    string SourceKey,
    int ItemsFetched,
    int CandidatesCreated,
    int DuplicatesSkipped,
    int KeywordFiltered,
    bool Succeeded,
    string? ErrorMessage);
