using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Hides existing forum content whose linked member was already suspended before topic-level
/// visibility was introduced.
/// </summary>
public partial class BackfillSuspendedMemberForumVisibility : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE post
            SET post.IsHidden = 1
            FROM dbo.ModernForumPost AS post
            INNER JOIN dbo.MemberAccounts AS member ON member.Id = post.AuthorMemberId
            WHERE member.IsSuspended = 1
              AND post.IsHidden = 0;
            """);

        migrationBuilder.Sql("""
            ;WITH RankedStarters AS
            (
                SELECT
                    post.ThreadId,
                    post.AuthorMemberId,
                    ROW_NUMBER() OVER
                    (
                        PARTITION BY post.ThreadId
                        ORDER BY post.LegacyPostId ASC
                    ) AS StarterRank
                FROM dbo.ModernForumPost AS post
            )
            UPDATE thread
            SET thread.IsHidden = 1
            FROM dbo.ModernForumThread AS thread
            INNER JOIN RankedStarters AS starter
                ON starter.ThreadId = thread.Id
               AND starter.StarterRank = 1
            INNER JOIN dbo.MemberAccounts AS member
                ON member.Id = starter.AuthorMemberId
            WHERE member.IsSuspended = 1
              AND thread.IsHidden = 0;
            """);

        migrationBuilder.Sql("EXEC dbo.ModernForum_RefreshReadStats;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally irreversible. The backfill cannot distinguish suspension visibility from
        // other moderation visibility, and restoring suspended content would violate the safety rule.
    }
}
