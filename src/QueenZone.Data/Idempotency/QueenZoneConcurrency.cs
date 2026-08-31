using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Shared optimistic-concurrency helpers for EF <see cref="SaveChangesAsync"/> and
/// SQL/ExecuteUpdate compare-and-swap writes. Lives next to
/// <see cref="QueenZoneDbTransactions"/> so write repositories share one pattern.
/// </summary>
internal static class QueenZoneConcurrency
{
    public static async Task SaveChangesAsync(
        QueenZoneDbContext dbContext,
        CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            foreach (var entry in exception.Entries)
            {
                await entry.ReloadAsync(cancellationToken);
            }

            throw new OptimisticConcurrencyException();
        }
    }

    public static void EnsureUpdated(int affectedRows, bool exists, string notFoundMessage)
    {
        if (affectedRows > 0)
        {
            return;
        }

        if (exists)
        {
            throw new OptimisticConcurrencyException();
        }

        throw new InvalidOperationException(notFoundMessage);
    }

    public static byte[] NewClientRowVersion() => Guid.NewGuid().ToByteArray();

    public static bool RowVersionEquals(byte[]? left, byte[]? right)
    {
        if (left is null || right is null)
        {
            return false;
        }

        return left.AsSpan().SequenceEqual(right);
    }
}
