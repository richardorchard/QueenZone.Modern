using Microsoft.EntityFrameworkCore;

namespace QueenZone.Data;

/// <summary>
/// Queries <see cref="QueenZoneDbContext.ModernForumPosts"/> directly rather than adding a
/// method to <see cref="IForumRepository"/>, since that interface has three implementations
/// (modern, legacy, in-memory test double) and this one count does not need the repository
/// abstraction.
/// </summary>
public sealed class EfLiveActivityQueryService(QueenZoneDbContext dbContext, TimeProvider timeProvider)
    : ILiveActivityQueryService
{
    public Task<int> GetNewForumRepliesTodayAsync(CancellationToken cancellationToken = default)
    {
        var todayStartUtc = timeProvider.GetUtcNow().UtcDateTime.Date;
        return dbContext.ModernForumPosts
            .Where(post => post.PostedAt != null && post.PostedAt >= todayStartUtc && !post.IsHidden)
            .CountAsync(cancellationToken);
    }
}
