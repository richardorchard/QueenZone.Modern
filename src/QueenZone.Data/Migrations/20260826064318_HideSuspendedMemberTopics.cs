using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QueenZone.Data.Migrations;

/// <summary>
/// Adds reversible topic visibility for member suspension and applies it to public forum reads.
/// </summary>
public partial class HideSuspendedMemberTopics : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            IF COL_LENGTH(N'dbo.ModernForumThread', N'IsHidden') IS NULL
                ALTER TABLE dbo.ModernForumThread ADD IsHidden bit NOT NULL
                    CONSTRAINT DF_ModernForumThread_IsHidden DEFAULT (0);
            """);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_RefreshReadStats
            AS
            BEGIN
                SET NOCOUNT ON;

                MERGE dbo.ModernForumCategoryReadStats AS target
                USING
                (
                    SELECT c.Id AS CategoryId, c.LegacyForumId,
                        COUNT_BIG(CASE WHEN t.IsLegacyTopicStarter = 1 AND t.IsHidden = 0 THEN 1 END) AS TotalThreads,
                        COUNT_BIG(CASE WHEN t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1 AND t.IsHidden = 0 THEN 1 END) AS ValidatedDisplayThreads
                    FROM dbo.ModernForumCategory c
                    LEFT JOIN dbo.ModernForumThread t ON t.CategoryId = c.Id
                    GROUP BY c.Id, c.LegacyForumId
                ) AS source ON target.CategoryId = source.CategoryId
                WHEN MATCHED THEN UPDATE SET LegacyForumId = source.LegacyForumId,
                    TotalThreads = CONVERT(int, source.TotalThreads),
                    ValidatedDisplayThreads = CONVERT(int, source.ValidatedDisplayThreads), UpdatedAt = sysutcdatetime()
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (CategoryId, LegacyForumId, TotalThreads, ValidatedDisplayThreads)
                    VALUES (source.CategoryId, source.LegacyForumId, CONVERT(int, source.TotalThreads), CONVERT(int, source.ValidatedDisplayThreads))
                WHEN NOT MATCHED BY SOURCE THEN DELETE;

                MERGE dbo.ModernForumThreadReadStats AS target
                USING
                (
                    SELECT t.Id AS ThreadId, t.LegacyTopicId,
                        COUNT_BIG(CASE WHEN p.IsHidden = 0 THEN p.Id END) AS PostCount
                    FROM dbo.ModernForumThread t
                    LEFT JOIN dbo.ModernForumPost p ON p.ThreadId = t.Id
                    GROUP BY t.Id, t.LegacyTopicId
                ) AS source ON target.ThreadId = source.ThreadId
                WHEN MATCHED THEN UPDATE SET LegacyTopicId = source.LegacyTopicId,
                    PostCount = CONVERT(int, source.PostCount), UpdatedAt = sysutcdatetime()
                WHEN NOT MATCHED BY TARGET THEN INSERT (ThreadId, LegacyTopicId, PostCount)
                    VALUES (source.ThreadId, source.LegacyTopicId, CONVERT(int, source.PostCount))
                WHEN NOT MATCHED BY SOURCE THEN DELETE;

                MERGE dbo.ModernForumArchiveReadStats AS target
                USING
                (
                    SELECT CONVERT(tinyint, 1) AS Id,
                        CONVERT(int, COUNT_BIG(*)) AS TotalThreads,
                        CONVERT(int, COUNT_BIG(CASE WHEN NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL THEN 1 END)) AS SitemapTopicCount
                    FROM dbo.ModernForumThread WHERE IsHidden = 0
                ) AS source ON target.Id = source.Id
                WHEN MATCHED THEN UPDATE SET TotalThreads = source.TotalThreads,
                    SitemapTopicCount = source.SitemapTopicCount, UpdatedAt = sysutcdatetime()
                WHEN NOT MATCHED BY TARGET THEN INSERT (Id, TotalThreads, SitemapTopicCount)
                    VALUES (source.Id, source.TotalThreads, source.SitemapTopicCount);
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetCategoryByLegacyForumId @Q_FORUM_ID int
            AS
            BEGIN
                SET NOCOUNT ON;
                SELECT c.LegacyForumId AS Id, c.Name,
                    NULLIF(LTRIM(RTRIM(c.Description)), '') AS Description,
                    c.LegacyPostCount AS PostCount, c.LastActivityAt,
                    latest.Title AS LatestThreadTitle, c.SortOrder
                FROM dbo.ModernForumCategory c
                OUTER APPLY
                (
                    SELECT TOP (1) t.Title
                    FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_CategoryStarter_Latest))
                    WHERE t.CategoryId = c.Id AND t.IsLegacyTopicStarter = 1 AND t.IsHidden = 0
                    ORDER BY t.LastActivityAt DESC, t.LegacyTopicId DESC
                ) latest
                WHERE c.LegacyForumId = @Q_FORUM_ID AND c.IsSynthetic = 0;
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetCategoryThreadsPage
                @CurrentPage int, @PageSize int, @Q_FORUM_ID int, @TotalRecords int OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @Offset int = (CASE WHEN @CurrentPage > 1 THEN @CurrentPage - 1 ELSE 0 END) * @PageSize;
                SELECT @TotalRecords = s.ValidatedDisplayThreads
                FROM dbo.ModernForumCategory c
                INNER JOIN dbo.ModernForumCategoryReadStats s ON s.CategoryId = c.Id
                WHERE c.LegacyForumId = @Q_FORUM_ID AND c.IsSynthetic = 0;
                IF @TotalRecords IS NULL
                    SELECT @TotalRecords = COUNT_BIG(*) FROM dbo.ModernForumThread t
                    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                    WHERE c.LegacyForumId = @Q_FORUM_ID AND c.IsSynthetic = 0
                      AND t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1 AND t.IsHidden = 0;
                SELECT CAST(0 AS int) AS Id, t.LegacyTopicId AS Q_FORUM_TOPIC_ID,
                    t.Title AS TOPIC_SUBJECT, t.LastActivityAt AS TOPIC_LAST_POST,
                    t.StartedByLegacyUserId AS USER_ID, t.StartedByDisplayName AS USERNAME,
                    t.ReplyCount AS NUMBEROFREPLIES, CAST(NULL AS nvarchar(100)) AS LAST_POST_USERNAME,
                    CAST(CASE WHEN t.IsSticky = 1 THEN 1 ELSE 0 END AS tinyint) AS STICKY
                FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_PublicCategoryPage))
                INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                WHERE c.LegacyForumId = @Q_FORUM_ID AND c.IsSynthetic = 0
                  AND t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1 AND t.IsHidden = 0
                ORDER BY t.IsSticky DESC, t.LastActivityAt DESC, t.LegacyTopicId ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicPostsPage
                @CurrentPage int, @PageSize int, @Q_FORUM_TOPIC_ID int,
                @TotalRecords int OUTPUT, @forum_name nvarchar(100) OUTPUT,
                @SUBJECT nvarchar(200) OUTPUT, @Q_FORUM_ID int OUTPUT,
                @DISCO tinyint OUTPUT, @HasPoll bit OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                DECLARE @Offset int = (CASE WHEN @CurrentPage > 1 THEN @CurrentPage - 1 ELSE 0 END) * @PageSize;
                DECLARE @ThreadId bigint;
                SET @HasPoll = 0;
                SELECT @ThreadId = t.Id, @SUBJECT = t.Title, @Q_FORUM_ID = c.LegacyForumId,
                    @forum_name = c.Name, @DISCO = t.LegacyDiscography
                FROM dbo.ModernForumThread t INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                WHERE t.LegacyTopicId = @Q_FORUM_TOPIC_ID AND t.IsHidden = 0;
                IF @ThreadId IS NULL BEGIN SET @TotalRecords = 0; RETURN; END;
                IF OBJECT_ID(N'dbo.ForumPolls', N'U') IS NOT NULL
                   AND EXISTS (SELECT 1 FROM dbo.ForumPolls WHERE LegacyTopicId = @Q_FORUM_TOPIC_ID)
                    SET @HasPoll = 1;
                SELECT @TotalRecords = PostCount FROM dbo.ModernForumThreadReadStats WHERE ThreadId = @ThreadId;
                IF @TotalRecords IS NULL
                    SELECT @TotalRecords = COUNT_BIG(*) FROM dbo.ModernForumPost p
                    WHERE p.ThreadId = @ThreadId AND p.IsHidden = 0;
                SELECT p.BodyHtml AS TOPIC_MESSAGE, p.PostedAt AS TOPIC_DATE,
                    p.AuthorLegacyUserId AS USER_ID, p.AuthorDisplayName AS USERNAME,
                    p.SignatureHtml AS SIGNATURE, p.AuthorPostCount AS NUMBER_OF_POSTS,
                    p.AuthorJoinedAt AS DATE_CREATED, p.LegacyPostId AS Q_FORUM_TOPIC_ID,
                    p.Attachment AS ATTACHMENT, p.FileSize AS FILESIZE, p.AttachCount AS ATTACH_COUNT,
                    CAST(0 AS tinyint) AS ONLINE, CAST(NULL AS varchar(50)) AS AVATAR,
                    CAST(NULL AS varchar(30)) AS DISPLAY_MESSAGE, p.LegacyDiscography AS DISCO,
                    p.AuthorMemberId, p.EditedAt, p.EditCount
                FROM dbo.ModernForumPost p WITH (INDEX(IX_ModernForumPost_Thread_Posted))
                WHERE p.ThreadId = @ThreadId AND p.IsHidden = 0
                ORDER BY p.LegacyPostId ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTotalThreadCount AS
            BEGIN SET NOCOUNT ON; SELECT COALESCE(
                (SELECT TotalThreads FROM dbo.ModernForumArchiveReadStats WHERE Id = 1),
                (SELECT CONVERT(int, COUNT_BIG(*)) FROM dbo.ModernForumThread WHERE IsHidden = 0)); END;
            """, suppressTransaction: true);
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicSitemapCount AS
            BEGIN SET NOCOUNT ON; SELECT COALESCE(
                (SELECT SitemapTopicCount FROM dbo.ModernForumArchiveReadStats WHERE Id = 1),
                (SELECT CONVERT(int, COUNT_BIG(*)) FROM dbo.ModernForumThread
                 WHERE IsHidden = 0 AND NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL)); END;
            """, suppressTransaction: true);
        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_GetTopicSitemapPage @Offset int, @PageSize int
            AS BEGIN SET NOCOUNT ON;
                SELECT t.LegacyTopicId AS TopicId, LTRIM(RTRIM(t.Title)) AS Title, t.LastActivityAt
                FROM dbo.ModernForumThread t WITH (INDEX(IX_ModernForumThread_Sitemap))
                WHERE t.IsHidden = 0 AND NULLIF(LTRIM(RTRIM(t.Title)), '') IS NOT NULL
                ORDER BY t.LegacyTopicId ASC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("""
            CREATE OR ALTER PROCEDURE dbo.ModernForum_SearchThreads
                @Query NVARCHAR(500), @Offset INT, @PageSize INT, @TotalRecords INT OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;
                ;WITH TitleMatches AS
                (
                    SELECT t.Id AS ThreadPk, ft.[RANK] AS SearchRank
                    FROM dbo.ModernForumThread t
                    INNER JOIN FREETEXTTABLE(dbo.ModernForumThread, Title, @Query) ft ON ft.[KEY] = t.Id
                    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                    WHERE t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1
                      AND t.IsHidden = 0 AND c.IsSynthetic = 0
                ),
                BodyMatches AS
                (
                    SELECT p.ThreadId AS ThreadPk, MAX(ft.[RANK]) AS SearchRank
                    FROM dbo.ModernForumPost p
                    INNER JOIN FREETEXTTABLE(dbo.ModernForumPost, BodyHtml, @Query) ft ON ft.[KEY] = p.Id
                    INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
                    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                    WHERE t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1
                      AND t.IsHidden = 0 AND p.IsHidden = 0 AND c.IsSynthetic = 0
                    GROUP BY p.ThreadId
                ),
                Combined AS
                (
                    SELECT COALESCE(tm.ThreadPk, bm.ThreadPk) AS ThreadPk,
                        COALESCE(tm.SearchRank, 0) + COALESCE(bm.SearchRank, 0) AS TotalRank
                    FROM TitleMatches tm FULL OUTER JOIN BodyMatches bm ON bm.ThreadPk = tm.ThreadPk
                )
                SELECT t.LegacyTopicId AS TopicId, LTRIM(RTRIM(t.Title)) AS Title,
                    c.LegacyForumId AS CategoryId, c.Name AS CategoryName,
                    ISNULL(t.ReplyCount, 0) AS ReplyCount, t.LastActivityAt, t.StartedByDisplayName
                FROM Combined r INNER JOIN dbo.ModernForumThread t ON t.Id = r.ThreadPk
                INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                ORDER BY r.TotalRank DESC, t.LastActivityAt DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;

                ;WITH TitleMatchIds AS
                (
                    SELECT ft.[KEY] AS ThreadPk
                    FROM FREETEXTTABLE(dbo.ModernForumThread, Title, @Query) ft
                    INNER JOIN dbo.ModernForumThread t ON t.Id = ft.[KEY]
                    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                    WHERE t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1
                      AND t.IsHidden = 0 AND c.IsSynthetic = 0
                ),
                BodyMatchIds AS
                (
                    SELECT DISTINCT p.ThreadId AS ThreadPk
                    FROM dbo.ModernForumPost p
                    INNER JOIN FREETEXTTABLE(dbo.ModernForumPost, BodyHtml, @Query) ft ON ft.[KEY] = p.Id
                    INNER JOIN dbo.ModernForumThread t ON t.Id = p.ThreadId
                    INNER JOIN dbo.ModernForumCategory c ON c.Id = t.CategoryId
                    WHERE t.IsLegacyTopicStarter = 1 AND t.StartedByUserValidated = 1
                      AND t.IsHidden = 0 AND p.IsHidden = 0 AND c.IsSynthetic = 0
                )
                SELECT @TotalRecords = COUNT(*) FROM
                (SELECT ThreadPk FROM TitleMatchIds UNION SELECT ThreadPk FROM BodyMatchIds) u;
            END;
            """, suppressTransaction: true);

        migrationBuilder.Sql("EXEC dbo.ModernForum_RefreshReadStats;", suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("Topic visibility is a safety control and cannot be removed automatically.");
    }
}
