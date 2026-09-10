using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Widens the member-linked forum post index to cover the ordering the public activity feed
/// uses, mirroring <c>IX_ModernForumPost_AuthorLegacyUserId_PostedAt</c> for linked members.
/// </summary>
/// <remarks>
/// <para>
/// <c>IX_ModernForumPost_AuthorMemberId</c> was a single key column, so
/// <c>WHERE AuthorMemberId = @id ORDER BY PostedAt DESC</c> could seek but not read in order.
/// The measured plan on the SQL Express mirror was a blocking
/// <c>Sort(TOP 20, ORDER BY PostedAt DESC)</c> over a nested loop with a clustered key lookup
/// per matching row — the sort had to consume every post a member had written before returning
/// a page of twenty.
/// </para>
/// <para>
/// With <c>PostedAt DESC</c> as a key column and the feed's remaining columns included, the same
/// single-author query becomes <c>Top(20)</c> over an <c>ORDERED FORWARD</c> index seek: no sort,
/// no lookups, and a cost independent of how many posts the member has. A feed covering several
/// authors still sorts, because the index orders within each <c>AuthorMemberId</c> rather than
/// across them, but it does so over a covering index with the lookups gone.
/// </para>
/// <para>
/// The filter matches the index being replaced. It keeps the index to the member-linked rows
/// (28 of 1,164,852 on the mirror, 16 KB) rather than the whole imported table, and SQL Server
/// still chooses it for a parameterised <c>AuthorMemberId = @id</c> predicate — verified on the
/// mirror, since an equality comparison already excludes NULL.
/// </para>
/// <para>
/// The old single-column index is dropped rather than kept: the replacement leads with the same
/// column under the same filter, so it serves every seek the old one did.
/// </para>
/// </remarks>
[DbContext(typeof(QueenZoneDbContext))]
[Migration("20260910120000_AddModernForumPostAuthorMemberPostedIndex")]
public partial class AddModernForumPostAuthorMemberPostedIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.ModernForumPost', N'AuthorMemberId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId_PostedAt')
            BEGIN
                CREATE INDEX IX_ModernForumPost_AuthorMemberId_PostedAt
                    ON dbo.ModernForumPost (AuthorMemberId, PostedAt DESC)
                    INCLUDE (ThreadId, AuthorDisplayName, IsHidden)
                    WHERE AuthorMemberId IS NOT NULL;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId')
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId_PostedAt')
            BEGIN
                DROP INDEX IX_ModernForumPost_AuthorMemberId
                    ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND COL_LENGTH(N'dbo.ModernForumPost', N'AuthorMemberId') IS NOT NULL
               AND NOT EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId')
            BEGIN
                CREATE INDEX IX_ModernForumPost_AuthorMemberId
                    ON dbo.ModernForumPost (AuthorMemberId)
                    WHERE AuthorMemberId IS NOT NULL;
            END
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            IF OBJECT_ID(N'dbo.ModernForumPost', N'U') IS NOT NULL
               AND EXISTS (
                    SELECT 1
                    FROM sys.indexes
                    WHERE object_id = OBJECT_ID(N'dbo.ModernForumPost', N'U')
                      AND name = N'IX_ModernForumPost_AuthorMemberId_PostedAt')
            BEGIN
                DROP INDEX IX_ModernForumPost_AuthorMemberId_PostedAt
                    ON dbo.ModernForumPost;
            END
            """, suppressTransaction: true);
    }
}
