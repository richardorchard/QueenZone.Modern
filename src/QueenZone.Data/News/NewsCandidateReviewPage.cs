namespace QueenZone.Data;

public sealed record NewsCandidateReviewPage(
    IReadOnlyList<NewsCandidateReviewListItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
