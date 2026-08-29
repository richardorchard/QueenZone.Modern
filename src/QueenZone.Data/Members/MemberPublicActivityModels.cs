namespace QueenZone.Data;

public static class MemberPublicActivityType
{
    public const string ForumPost = "Forum post";

    public const string Article = "Article";

    public const string News = "News";

    public const string Photo = "Photo";
}

public sealed record MemberPublicActivityItem(
    string Type,
    string Title,
    string? Summary,
    DateTimeOffset PublishedAt,
    int? ContentId = null,
    int? ParentId = null,
    string? Slug = null,
    string? Category = null,
    Guid? AuthorId = null,
    string? AuthorDisplayName = null);

public sealed record MemberPublicActivityPage(
    IReadOnlyList<MemberPublicActivityItem> Items,
    int TotalCount,
    int Page,
    int PageSize);
