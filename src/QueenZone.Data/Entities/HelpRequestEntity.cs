using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

[ExcludeFromCodeCoverage]
public sealed class HelpRequestEntity
{
    public Guid Id { get; set; }

    public string Topic { get; set; } = string.Empty;

    public string Subject { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string NormalizedEmail { get; set; } = string.Empty;

    public Guid? MemberId { get; set; }

    public string Status { get; set; } = HelpRequestStatus.Open;

    public DateTimeOffset SubmittedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewerEmail { get; set; }

    public string? ReviewNotes { get; set; }

    public MemberAccount? Member { get; set; }
}
