using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using QueenZone.Data;

namespace QueenZone.Web.Tests;

public sealed class ForumVisibilityMigrationTests
{
    [Fact]
    public void BackfillSuspendedMemberForumVisibility_GeneratesExpectedSqlServerUpdates()
    {
        var options = new DbContextOptionsBuilder<QueenZoneDbContext>()
            .UseSqlServer("Server=localhost;Database=MigrationSqlShape;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var dbContext = new QueenZoneDbContext(options);
        var migrator = dbContext.Database.GetService<IMigrator>();

        var sql = migrator.GenerateScript(
            "20260826104114_AddNewsAgentGuidanceRevisions",
            "20260827131746_BackfillSuspendedMemberForumVisibility");

        Assert.Contains("INNER JOIN dbo.MemberAccounts AS member ON member.Id = post.AuthorMemberId", sql);
        Assert.Contains("WHERE member.IsSuspended = 1", sql);
        Assert.Contains("ROW_NUMBER() OVER", sql);
        Assert.Contains("ORDER BY post.LegacyPostId ASC", sql);
        Assert.Contains("SET thread.IsHidden = 1", sql);
        Assert.Contains("SET post.IsHidden = 1", sql);
        Assert.Contains("EXEC dbo.ModernForum_RefreshReadStats", sql);
    }
}
