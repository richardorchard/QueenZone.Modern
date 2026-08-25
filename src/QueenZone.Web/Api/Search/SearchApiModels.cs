namespace QueenZone.Web;

/// <summary>
/// One mixed-content search hit for <c>GET /api/v1/search</c>. <c>Summary</c> is the
/// index excerpt (plain text). <c>Id</c> is parsed from numeric source keys
/// (<c>news:123</c>, <c>forum-thread:4521</c>); slug keys such as
/// <c>article:some-slug</c> leave it null.
/// </summary>
public sealed record SearchResultDto(
    string ContentType,
    string SourceKey,
    string Title,
    string Summary,
    string Url,
    DateTimeOffset? PublishedAt,
    string? ImageUrl,
    string? Category,
    string? AuthorDisplayName,
    int? Id);
