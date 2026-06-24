namespace QueenZone.Data;

public sealed record ForumArchiveStats(
    int ForumCount,
    int ThreadCount,
    long PostCount)
{
    public static ForumArchiveStats FromCategories(
        IReadOnlyList<ForumCategoryItem> categories,
        int threadCount) =>
        new(
            categories.Count,
            threadCount,
            categories.Sum(category => (long)category.PostCount));
}