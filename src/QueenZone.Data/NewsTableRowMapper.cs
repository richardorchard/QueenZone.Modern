using QueenZone.Data.Entities;

namespace QueenZone.Data;

internal static class NewsTableRowMapper
{
    public static AdminNewsArticle ToAdminArticle(NewsTableRow row) =>
        new(
            row.NewsId,
            row.Title,
            row.Slug ?? string.Empty,
            row.Excerpt,
            row.Body,
            row.PublishedAt,
            row.SourceUrl,
            row.IsPublished,
            row.CreatedAt,
            row.UpdatedAt,
            row.EditorEmail);
}