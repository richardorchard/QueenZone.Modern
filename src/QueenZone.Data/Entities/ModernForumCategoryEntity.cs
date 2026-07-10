namespace QueenZone.Data.Entities;

public sealed class ModernForumCategoryEntity
{
    public int Id { get; set; }

    public int LegacyForumId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public int LegacyPostCount { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public bool IsSynthetic { get; set; }

    public DateTime ImportedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
