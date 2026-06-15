namespace QueenZone.Data.Entities;

public sealed class NewsTableRow
{
    public int NewsId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Excerpt { get; set; } = string.Empty;

    public string Body { get; set; } = string.Empty;

    public DateTime PublishedAt { get; set; }

    public string? SourceUrl { get; set; }

    public bool IsPublished { get; set; }

    public string? Slug { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? EditorEmail { get; set; }

    public int? UserId { get; set; }

    public int Type { get; set; }

    public int QueenOnline { get; set; }
}