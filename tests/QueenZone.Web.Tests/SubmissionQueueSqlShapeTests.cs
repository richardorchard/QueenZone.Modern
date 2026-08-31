using Microsoft.EntityFrameworkCore;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class SubmissionQueueSqlShapeTests
{
    [Fact]
    public void SqlServer_queue_queries_emit_offset_fetch_next()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=QueenZonePagingShape;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new QueenZoneDbContext(options);
        var memberId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        AssertPaged(new EfArticleSubmissionRepository(dbContext).PendingQueueQuery(10, 10).ToQueryString());
        AssertPaged(new EfArticleSubmissionRepository(dbContext).MemberDraftsSqlQuery(10, 10, memberId).ToQueryString());
        AssertPaged(new EfPhotoSubmissionRepository(dbContext).PendingQueueQuery(10, 10).ToQueryString());
        AssertPaged(new EfPhotoSubmissionRepository(dbContext).MemberQueueQuery(memberId, 10, 10).ToQueryString());
        AssertPaged(new EfNewsSuggestionRepository(dbContext).PendingQueueQuery(10, 10).ToQueryString());
        AssertPaged(new EfNewsSuggestionRepository(dbContext).MemberQueueQuery(memberId, 10, 10).ToQueryString());
        AssertPaged(new EfTriviaFactSubmissionRepository(dbContext).PendingQueueQuery(10, 10).ToQueryString());
        AssertPaged(new EfTriviaFactSubmissionRepository(dbContext).MemberQueueQuery(memberId, 10, 10).ToQueryString());
    }

    private static void AssertPaged(string sql)
    {
        Assert.Contains("OFFSET", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FETCH", sql, StringComparison.OrdinalIgnoreCase);
    }
}
