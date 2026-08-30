namespace QueenZone.Data;

public static class IdempotencyOperationKinds
{
    public const string ForumCreateTopic = "forum.createTopic";

    public const string ForumCreateReply = "forum.createReply";

    public const string MessageCompose = "message.compose";

    public const string MessageReply = "message.reply";
}

public static class IdempotencyLimits
{
    public static readonly TimeSpan ReceiptLifetime = TimeSpan.FromDays(7);

    public const int OperationKindMaxLength = 64;

    public const int PayloadHashLength = 64;

    public const int LocationMaxLength = 2048;
}

/// <summary>Original success snapshot stored for idempotent replay.</summary>
public sealed record IdempotencyReceipt(
    int StatusCode,
    string? Location,
    string ResponseBodyJson,
    string PayloadHash);

public enum IdempotencyExecuteKind
{
    Replay,
    Conflict,
    Ran,
}

public sealed class IdempotencyExecuteResult<T>
{
    public IdempotencyExecuteKind Kind { get; private init; }

    public T? Result { get; private init; }

    public IdempotencyReceipt? Receipt { get; private init; }

    public static IdempotencyExecuteResult<T> Replay(IdempotencyReceipt receipt) =>
        new() { Kind = IdempotencyExecuteKind.Replay, Receipt = receipt };

    public static IdempotencyExecuteResult<T> Conflict() =>
        new() { Kind = IdempotencyExecuteKind.Conflict };

    public static IdempotencyExecuteResult<T> Ran(T result) =>
        new() { Kind = IdempotencyExecuteKind.Ran, Result = result };
}

public interface IIdempotencyStore
{
    /// <summary>
    /// Looks up an existing success receipt or runs <paramref name="action"/>.
    /// On the SQL path the write and receipt share one DbContext transaction.
    /// Failed writes (<c>Success</c> null) do not persist a receipt. Expired
    /// receipts are treated as unknown.
    /// </summary>
    Task<IdempotencyExecuteResult<T>> ExecuteAsync<T>(
        Guid memberId,
        string operationKind,
        Guid operationId,
        string payloadHash,
        Func<CancellationToken, Task<(T Result, IdempotencyReceipt? Success)>> action,
        CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredAsync(CancellationToken cancellationToken = default);
}
