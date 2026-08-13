namespace QueenZone.Data;

public sealed record PhotoCategoryPage(string CategoryName, IReadOnlyList<PhotoItem> Items, int TotalCount);
