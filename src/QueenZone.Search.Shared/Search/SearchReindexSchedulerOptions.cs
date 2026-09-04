namespace QueenZone.Search.Shared;

public sealed class SearchReindexSchedulerOptions
{
    public const string SectionName = "SearchReindexScheduler";

    public bool UseRunLease { get; init; } = true;

    public string LeaseName { get; init; } = "search-reindex";

    public int LeaseDurationMinutes { get; init; } = 60;
}
