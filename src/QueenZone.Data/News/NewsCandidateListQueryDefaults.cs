namespace QueenZone.Data;

public static class NewsCandidateListQueryDefaults
{
    public const int PageSize = 50;

    public const int MaxPageSize = 100;

    public static (int Page, int PageSize) Normalize(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));
}
