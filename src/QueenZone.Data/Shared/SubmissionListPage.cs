namespace QueenZone.Data;

/// <summary>Paginated member or admin submission list result.</summary>
public sealed record SubmissionListPage<T>(IReadOnlyList<T> Items, int TotalCount);
