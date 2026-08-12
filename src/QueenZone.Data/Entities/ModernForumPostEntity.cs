using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class ModernForumPostEntity
{
    public long Id { get; set; }

    public int LegacyPostId { get; set; }

    public int LegacyThreadTopicId { get; set; }

    public long ThreadId { get; set; }

    public int LegacyForumId { get; set; }

    public int? AuthorLegacyUserId { get; set; }

    public Guid? AuthorMemberId { get; set; }

    public string AuthorDisplayName { get; set; } = string.Empty;

    public int? AuthorPostCount { get; set; }

    public DateTime? AuthorJoinedAt { get; set; }

    public string BodyHtml { get; set; } = string.Empty;

    public string? SignatureHtml { get; set; }

    public DateTime? PostedAt { get; set; }

    public DateTime? EditedAt { get; set; }

    public int EditCount { get; set; }

    public byte LegacyDiscography { get; set; }

    public bool? AuthorUserValidated { get; set; }

    public string? Attachment { get; set; }

    public string? FileSize { get; set; }

    public int AttachCount { get; set; }

    public DateTime ImportedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// True when an admin suspended the author's account. Hidden posts are excluded from
    /// public thread views, member activity, and the write-side post/count queries below;
    /// the row itself is kept so reinstating the member restores visibility.
    /// </summary>
    public bool IsHidden { get; set; }

    public ModernForumThreadEntity? Thread { get; set; }
}
