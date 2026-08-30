using System.Diagnostics.CodeAnalysis;

namespace QueenZone.Data.Entities;

/// <summary>
/// Persists the original success of a member write so the same
/// <c>Idempotency-Key</c> can be replayed without duplicating the resource.
/// Uniqueness is (<see cref="MemberId"/>, <see cref="OperationKind"/>,
/// <see cref="OperationId"/>).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class IdempotencyReceiptEntity
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }

    public string OperationKind { get; set; } = string.Empty;

    public Guid OperationId { get; set; }

    public string PayloadHash { get; set; } = string.Empty;

    public int StatusCode { get; set; }

    public string? Location { get; set; }

    public string ResponseBodyJson { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
