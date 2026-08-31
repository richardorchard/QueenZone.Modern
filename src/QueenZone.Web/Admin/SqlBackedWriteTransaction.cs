using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QueenZone.Data;

namespace QueenZone.Web;

/// <summary>
/// Runs admin write workflows inside an EF execution-strategy transaction when a
/// <see cref="QueenZoneDbContext"/> is registered (SQL-backed hosts). In-memory
/// Testing hosts have no DbContext, so the action runs without a transaction.
/// </summary>
internal static class SqlBackedWriteTransaction
{
    public static async Task<T> ExecuteAsync<T>(
        IServiceProvider? serviceProvider,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken)
    {
        if (serviceProvider?.GetService<QueenZoneDbContext>() is not { } dbContext)
        {
            return await action(cancellationToken);
        }

        var strategy = dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var result = await action(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
