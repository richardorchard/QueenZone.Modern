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

    /// <summary>
    /// Blob key or prefixed gallery reference. Never image bytes.
    /// </summary>
    public string? ImageBlobKey { get; set; }

    /// <summary>
    /// Optional <c>PIC_FILES_T</c> pick for later gallery-backed article images.
    /// </summary>
    public int? ImageGalleryPicId { get; set; }
}
