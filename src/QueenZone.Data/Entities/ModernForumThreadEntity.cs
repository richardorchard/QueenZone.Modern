using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class ModernForumThreadEntity
{
    public long Id { get; set; }

    public int LegacyTopicId { get; set; }

    public int LegacyForumId { get; set; }

    public int CategoryId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int? StartedByLegacyUserId { get; set; }

    public string StartedByDisplayName { get; set; } = string.Empty;

    public DateTime? StartedAt { get; set; }

    public DateTime? LastActivityAt { get; set; }

    public int ReplyCount { get; set; }

    public bool IsSticky { get; set; }

    public bool IsLegacyTopicStarter { get; set; }

    public byte LegacyDiscography { get; set; }

    public bool? StartedByUserValidated { get; set; }

    public string? StarterAttachment { get; set; }

    public string? StarterFileSize { get; set; }

    public int StarterAttachCount { get; set; }

    public DateTime ImportedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ModernForumCategoryEntity? Category { get; set; }
}
