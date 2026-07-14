using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfForumWriteRepository(QueenZoneDbContext dbContext) : IForumWriteRepository
{
    private const int BodyHtmlMaxLength = 8000;

    public async Task<ForumThreadCreateResult> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default)
    {
        var now = ToUtcDateTime(thread.CreatedAt);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var category = await dbContext.ModernForumCategories
            .SingleOrDefaultAsync(item => item.LegacyForumId == thread.CategoryId && !item.IsSynthetic, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException("Forum category not found.");
        }

        var topicId = await GetNextTopicIdAsync(cancellationToken);
        var postId = await GetNextPostIdAsync(cancellationToken);

        var forumThread = new ModernForumThreadEntity
        {
            LegacyTopicId = topicId,
            LegacyForumId = category.LegacyForumId,
            CategoryId = category.Id,
            Title = thread.Subject.Trim(),
            StartedByDisplayName = thread.AuthorDisplayName.Trim(),
            StartedAt = now,
            LastActivityAt = now,
            ReplyCount = 0,
            IsSticky = false,
            IsLegacyTopicStarter = true,
            LegacyDiscography = 0,
            StartedByUserValidated = true,
            StarterAttachCount = 0,
            ImportedAt = now,
            UpdatedAt = now,
        };

        dbContext.ModernForumThreads.Add(forumThread);
        await dbContext.SaveChangesAsync(cancellationToken);

        dbContext.ModernForumPosts.Add(new ModernForumPostEntity
        {
            LegacyPostId = postId,
            LegacyThreadTopicId = topicId,
            ThreadId = forumThread.Id,
            LegacyForumId = category.LegacyForumId,
            AuthorDisplayName = thread.AuthorDisplayName.Trim(),
            BodyHtml = TruncateBody(thread.Body),
            PostedAt = now,
            LegacyDiscography = 0,
            AuthorUserValidated = true,
            AttachCount = 0,
            ImportedAt = now,
            UpdatedAt = now,
        });

        if (thread.Poll is not null)
        {
            var poll = EfForumPollRepository.BuildPollEntity(
                forumThread.Id,
                topicId,
                thread.Poll with { CreatedByMemberId = thread.AuthorMemberId },
                thread.CreatedAt);
            dbContext.ForumPolls.Add(poll);
        }

        category.LegacyPostCount += 1;
        category.LastActivityAt = now;
        category.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);
        await RefreshStatsForThreadAsync(forumThread.Id, topicId, category.Id, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ForumThreadCreateResult(topicId, postId);
    }

    public async Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default)
    {
        var now = ToUtcDateTime(post.CreatedAt);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var thread = await dbContext.ModernForumThreads
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.LegacyTopicId == post.TopicId, cancellationToken);
        if (thread is null)
        {
            throw new InvalidOperationException("Forum thread not found.");
        }

        var postId = await GetNextPostIdAsync(cancellationToken);
        dbContext.ModernForumPosts.Add(new ModernForumPostEntity
        {
            LegacyPostId = postId,
            LegacyThreadTopicId = thread.LegacyTopicId,
            ThreadId = thread.Id,
            LegacyForumId = thread.LegacyForumId,
            AuthorDisplayName = post.AuthorDisplayName.Trim(),
            BodyHtml = TruncateBody(post.Body),
            PostedAt = now,
            LegacyDiscography = thread.LegacyDiscography,
            AuthorUserValidated = true,
            AttachCount = 0,
            ImportedAt = now,
            UpdatedAt = now,
        });

        thread.ReplyCount += 1;
        thread.LastActivityAt = now;
        thread.UpdatedAt = now;
        if (thread.Category is not null)
        {
            thread.Category.LegacyPostCount += 1;
            thread.Category.LastActivityAt = now;
            thread.Category.UpdatedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await RefreshStatsForThreadAsync(thread.Id, thread.LegacyTopicId, thread.CategoryId, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return postId;
    }

    public async Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default) =>
        await dbContext.ModernForumThreads
            .AsNoTracking()
            .Where(thread => thread.LegacyTopicId == topicId)
            .Select(thread => new ForumWriteThread(
                thread.LegacyTopicId,
                thread.LegacyForumId,
                thread.Title,
                thread.StartedAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(thread.StartedAt.Value, DateTimeKind.Utc)) : DateTimeOffset.MinValue,
                thread.LastActivityAt.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(thread.LastActivityAt.Value, DateTimeKind.Utc)) : DateTimeOffset.MinValue,
                thread.ReplyCount + 1,
                IsLocked: false))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        var displayName = await GetDisplayNameAsync(memberId, cancellationToken);
        if (displayName is null)
        {
            return 0;
        }

        var sinceUtc = ToUtcDateTime(since);
        return await dbContext.ModernForumPosts
            .AsNoTracking()
            .CountAsync(post => post.AuthorDisplayName == displayName && post.PostedAt >= sinceUtc, cancellationToken);
    }

    public async Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        var displayName = await GetDisplayNameAsync(memberId, cancellationToken);
        if (displayName is null)
        {
            return 0;
        }

        return await dbContext.ModernForumPosts
            .AsNoTracking()
            .CountAsync(post => post.AuthorDisplayName == displayName, cancellationToken);
    }

    private async Task<int> GetNextTopicIdAsync(CancellationToken cancellationToken)
    {
        var maxTopicId = await dbContext.ModernForumThreads.MaxAsync(thread => (int?)thread.LegacyTopicId, cancellationToken) ?? 0;
        return maxTopicId + 1;
    }

    private async Task<int> GetNextPostIdAsync(CancellationToken cancellationToken)
    {
        var maxPostId = await dbContext.ModernForumPosts.MaxAsync(post => (int?)post.LegacyPostId, cancellationToken) ?? 0;
        return maxPostId + 1;
    }

    private async Task<string?> GetDisplayNameAsync(Guid memberId, CancellationToken cancellationToken) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(member => member.Id == memberId)
            .Select(member => member.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

    [ExcludeFromCodeCoverage(Justification = "SQL Server read-stat maintenance is covered by manual/production smoke checks; SQLite tests exercise the write flow.")]
    private async Task RefreshStatsForThreadAsync(
        long threadId,
        int legacyTopicId,
        int categoryId,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
        {
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            IF OBJECT_ID(N'dbo.ModernForumThreadReadStats', N'U') IS NOT NULL
            BEGIN
                MERGE dbo.ModernForumThreadReadStats AS target
                USING
                (
                    SELECT
                        {threadId} AS ThreadId,
                        {legacyTopicId} AS LegacyTopicId,
                        CONVERT(int, COUNT_BIG(*)) AS PostCount
                    FROM dbo.ModernForumPost
                    WHERE ThreadId = {threadId}
                ) AS source
                ON target.ThreadId = source.ThreadId
                WHEN MATCHED THEN
                    UPDATE SET PostCount = source.PostCount, UpdatedAt = {updatedAt}
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (ThreadId, LegacyTopicId, PostCount, UpdatedAt)
                    VALUES (source.ThreadId, source.LegacyTopicId, source.PostCount, {updatedAt});
            END;

            IF OBJECT_ID(N'dbo.ModernForumCategoryReadStats', N'U') IS NOT NULL
            BEGIN
                MERGE dbo.ModernForumCategoryReadStats AS target
                USING
                (
                    SELECT
                        {categoryId} AS CategoryId,
                        CONVERT(int, COUNT_BIG(*)) AS TotalThreads,
                        CONVERT(int, COUNT_BIG(CASE WHEN StartedByUserValidated = 1 THEN 1 END)) AS ValidatedDisplayThreads
                    FROM dbo.ModernForumThread
                    WHERE CategoryId = {categoryId}
                      AND IsLegacyTopicStarter = 1
                ) AS source
                ON target.CategoryId = source.CategoryId
                WHEN MATCHED THEN
                    UPDATE SET
                        TotalThreads = source.TotalThreads,
                        ValidatedDisplayThreads = source.ValidatedDisplayThreads,
                        UpdatedAt = {updatedAt}
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (CategoryId, TotalThreads, ValidatedDisplayThreads, UpdatedAt)
                    VALUES (source.CategoryId, source.TotalThreads, source.ValidatedDisplayThreads, {updatedAt});
            END;

            IF OBJECT_ID(N'dbo.ModernForumArchiveReadStats', N'U') IS NOT NULL
            BEGIN
                MERGE dbo.ModernForumArchiveReadStats AS target
                USING
                (
                    SELECT
                        CONVERT(tinyint, 1) AS Id,
                        CONVERT(int, COUNT_BIG(*)) AS TotalThreads,
                        CONVERT(int, COUNT_BIG(CASE WHEN NULLIF(LTRIM(RTRIM(Title)), '') IS NOT NULL THEN 1 END)) AS SitemapTopicCount
                    FROM dbo.ModernForumThread
                ) AS source
                ON target.Id = source.Id
                WHEN MATCHED THEN
                    UPDATE SET
                        TotalThreads = source.TotalThreads,
                        SitemapTopicCount = source.SitemapTopicCount,
                        UpdatedAt = {updatedAt}
                WHEN NOT MATCHED BY TARGET THEN
                    INSERT (Id, TotalThreads, SitemapTopicCount, UpdatedAt)
                    VALUES (source.Id, source.TotalThreads, source.SitemapTopicCount, {updatedAt});
            END;
            """, cancellationToken);
    }

    private static DateTime ToUtcDateTime(DateTimeOffset value) =>
        value.UtcDateTime;

    private static string TruncateBody(string body) =>
        body.Length <= BodyHtmlMaxLength ? body : body[..BodyHtmlMaxLength];
}
