using System.Data;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using QueenZone.Data.Entities;

namespace QueenZone.Data;

public sealed class EfForumWriteRepository(QueenZoneDbContext dbContext) : IForumWriteRepository
{
    private const int BodyHtmlMaxLength = 8000;
    internal const string TopicIdSequence = "ForumLegacyTopicIdSeq";
    internal const string PostIdSequence = "ForumLegacyPostIdSeq";

    public async Task<ForumThreadCreateResult> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default)
    {
        var now = ToUtcDateTime(thread.CreatedAt);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var category = await dbContext.ModernForumCategories
            .SingleOrDefaultAsync(item => item.LegacyForumId == thread.CategoryId && !item.IsSynthetic, cancellationToken);
        if (category is null)
        {
            throw new InvalidOperationException("Forum category not found.");
        }

        var topicId = await AllocateNextTopicIdAsync(cancellationToken);
        var postId = await AllocateNextPostIdAsync(cancellationToken);

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

        var firstPost = new ModernForumPostEntity
        {
            LegacyPostId = postId,
            LegacyThreadTopicId = topicId,
            Thread = forumThread,
            LegacyForumId = category.LegacyForumId,
            AuthorMemberId = thread.AuthorMemberId,
            AuthorDisplayName = thread.AuthorDisplayName.Trim(),
            BodyHtml = TruncateBody(thread.Body),
            PostedAt = now,
            LegacyDiscography = 0,
            AuthorUserValidated = true,
            AttachCount = 0,
            EditCount = 0,
            ImportedAt = now,
            UpdatedAt = now,
        };

        dbContext.ModernForumThreads.Add(forumThread);
        dbContext.ModernForumPosts.Add(firstPost);

        if (thread.Poll is not null)
        {
            var poll = EfForumPollRepository.BuildPollEntity(
                forumThread,
                thread.Poll with { CreatedByMemberId = thread.AuthorMemberId },
                thread.CreatedAt);
            dbContext.ForumPolls.Add(poll);
        }

        category.LegacyPostCount += 1;
        category.LastActivityAt = now;
        category.UpdatedAt = now;

        await dbContext.SaveChangesAsync(cancellationToken);

        await ApplyCreateThreadStatsAsync(
            forumThread.Id,
            topicId,
            category.Id,
            now,
            titleCountsForSitemap: !string.IsNullOrWhiteSpace(forumThread.Title),
            cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new ForumThreadCreateResult(topicId, postId);
    }

    public async Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default)
    {
        var now = ToUtcDateTime(post.CreatedAt);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var thread = await dbContext.ModernForumThreads
            .Include(item => item.Category)
            .SingleOrDefaultAsync(item => item.LegacyTopicId == post.TopicId, cancellationToken);
        if (thread is null)
        {
            throw new InvalidOperationException("Forum thread not found.");
        }

        var postId = await AllocateNextPostIdAsync(cancellationToken);
        dbContext.ModernForumPosts.Add(new ModernForumPostEntity
        {
            LegacyPostId = postId,
            LegacyThreadTopicId = thread.LegacyTopicId,
            ThreadId = thread.Id,
            LegacyForumId = thread.LegacyForumId,
            AuthorMemberId = post.AuthorMemberId,
            AuthorDisplayName = post.AuthorDisplayName.Trim(),
            BodyHtml = TruncateBody(post.Body),
            PostedAt = now,
            LegacyDiscography = thread.LegacyDiscography,
            AuthorUserValidated = true,
            AttachCount = 0,
            EditCount = 0,
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
        await ApplyCreatePostStatsAsync(thread.Id, thread.LegacyTopicId, now, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return postId;
    }

    public async Task<ForumEditablePost?> GetPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        var row = await dbContext.ModernForumPosts
            .AsNoTracking()
            .Where(post => post.LegacyPostId == postId)
            .Select(post => new
            {
                post.LegacyPostId,
                post.LegacyThreadTopicId,
                TopicSubject = post.Thread!.Title,
                post.BodyHtml,
                post.AuthorMemberId,
                post.PostedAt,
                post.EditedAt,
                post.EditCount,
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        var position = await dbContext.ModernForumPosts
            .AsNoTracking()
            .CountAsync(
                post => post.LegacyThreadTopicId == row.LegacyThreadTopicId && post.LegacyPostId <= postId,
                cancellationToken);

        return new ForumEditablePost(
            row.LegacyPostId,
            row.LegacyThreadTopicId,
            row.TopicSubject,
            row.BodyHtml,
            row.AuthorMemberId,
            ToOffset(row.PostedAt),
            row.EditedAt.HasValue ? ToOffset(row.EditedAt) : null,
            row.EditCount,
            Math.Max(1, position));
    }

    public async Task<ForumPostUpdateResult> UpdatePostAsync(
        int postId,
        Guid editorMemberId,
        string sanitisedBody,
        bool isAdmin,
        int editWindowMinutes,
        CancellationToken cancellationToken = default)
    {
        var post = await dbContext.ModernForumPosts
            .Include(item => item.Thread)
            .SingleOrDefaultAsync(item => item.LegacyPostId == postId, cancellationToken);
        if (post?.Thread is null)
        {
            return new ForumPostUpdateResult(ForumPostUpdateStatus.NotFound);
        }

        var postedAt = ToOffset(post.PostedAt);
        var canEdit = ForumPostEditRules.CanEdit(
            post.AuthorMemberId,
            editorMemberId,
            isAdmin,
            postedAt,
            editWindowMinutes,
            DateTimeOffset.UtcNow);

        if (!canEdit)
        {
            if (!isAdmin
                && post.AuthorMemberId == editorMemberId
                && editWindowMinutes == 0)
            {
                return new ForumPostUpdateResult(ForumPostUpdateStatus.EditingDisabled, post.LegacyThreadTopicId, post.Thread.Title);
            }

            if (!isAdmin
                && post.AuthorMemberId == editorMemberId
                && editWindowMinutes > 0
                && DateTimeOffset.UtcNow > postedAt.AddMinutes(editWindowMinutes))
            {
                return new ForumPostUpdateResult(ForumPostUpdateStatus.EditWindowExpired, post.LegacyThreadTopicId, post.Thread.Title);
            }

            return new ForumPostUpdateResult(ForumPostUpdateStatus.Forbidden, post.LegacyThreadTopicId, post.Thread.Title);
        }

        var now = DateTime.UtcNow;
        post.BodyHtml = TruncateBody(sanitisedBody);
        post.EditedAt = now;
        post.EditCount += 1;
        post.UpdatedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ForumPostUpdateResult(ForumPostUpdateStatus.Success, post.LegacyThreadTopicId, post.Thread.Title);
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

    private async Task<int> AllocateNextTopicIdAsync(CancellationToken cancellationToken) =>
        await AllocateNextLegacyIdAsync(
            TopicIdSequence,
            static async (db, ct) =>
                (await db.ModernForumThreads.MaxAsync(thread => (int?)thread.LegacyTopicId, ct) ?? 0) + 1,
            cancellationToken);

    private async Task<int> AllocateNextPostIdAsync(CancellationToken cancellationToken) =>
        await AllocateNextLegacyIdAsync(
            PostIdSequence,
            static async (db, ct) =>
                (await db.ModernForumPosts.MaxAsync(post => (int?)post.LegacyPostId, ct) ?? 0) + 1,
            cancellationToken);

    private async Task<int> AllocateNextLegacyIdAsync(
        string sequenceName,
        Func<QueenZoneDbContext, CancellationToken, Task<int>> fallback,
        CancellationToken cancellationToken)
    {
        if (!IsSqlServer())
        {
            return await fallback(dbContext, cancellationToken);
        }

        var connection = dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT NEXT VALUE FOR dbo.[{sequenceName}]";
        if (dbContext.Database.CurrentTransaction is { } transaction)
        {
            command.Transaction = transaction.GetDbTransaction();
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        if (result is null or DBNull)
        {
            return await fallback(dbContext, cancellationToken);
        }

        return Convert.ToInt32(result);
    }

    private async Task<string?> GetDisplayNameAsync(Guid memberId, CancellationToken cancellationToken) =>
        await dbContext.MemberAccounts
            .AsNoTracking()
            .Where(member => member.Id == memberId)
            .Select(member => member.DisplayName)
            .SingleOrDefaultAsync(cancellationToken);

    [ExcludeFromCodeCoverage(Justification = "SQL Server read-stat maintenance is covered by manual/production smoke checks; SQLite tests exercise the write flow.")]
    private async Task ApplyCreateThreadStatsAsync(
        long threadId,
        int legacyTopicId,
        int categoryId,
        DateTime updatedAt,
        bool titleCountsForSitemap,
        CancellationToken cancellationToken)
    {
        if (!IsSqlServer())
        {
            return;
        }

        var sitemapDelta = titleCountsForSitemap ? 1 : 0;
        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            IF OBJECT_ID(N'dbo.ModernForumThreadReadStats', N'U') IS NOT NULL
            BEGIN
                INSERT INTO dbo.ModernForumThreadReadStats (ThreadId, LegacyTopicId, PostCount, UpdatedAt)
                VALUES ({threadId}, {legacyTopicId}, 1, {updatedAt});
            END;

            IF OBJECT_ID(N'dbo.ModernForumCategoryReadStats', N'U') IS NOT NULL
            BEGIN
                UPDATE dbo.ModernForumCategoryReadStats
                SET TotalThreads = TotalThreads + 1,
                    ValidatedDisplayThreads = ValidatedDisplayThreads + 1,
                    UpdatedAt = {updatedAt}
                WHERE CategoryId = {categoryId};
            END;

            IF OBJECT_ID(N'dbo.ModernForumArchiveReadStats', N'U') IS NOT NULL
            BEGIN
                UPDATE dbo.ModernForumArchiveReadStats
                SET TotalThreads = TotalThreads + 1,
                    SitemapTopicCount = SitemapTopicCount + {sitemapDelta},
                    UpdatedAt = {updatedAt}
                WHERE Id = 1;
            END;
            """, cancellationToken);
    }

    [ExcludeFromCodeCoverage(Justification = "SQL Server read-stat maintenance is covered by manual/production smoke checks; SQLite tests exercise the write flow.")]
    private async Task ApplyCreatePostStatsAsync(
        long threadId,
        int legacyTopicId,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        if (!IsSqlServer())
        {
            return;
        }

        await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
            IF OBJECT_ID(N'dbo.ModernForumThreadReadStats', N'U') IS NOT NULL
            BEGIN
                UPDATE dbo.ModernForumThreadReadStats
                SET PostCount = PostCount + 1,
                    UpdatedAt = {updatedAt}
                WHERE ThreadId = {threadId};

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO dbo.ModernForumThreadReadStats (ThreadId, LegacyTopicId, PostCount, UpdatedAt)
                    SELECT
                        {threadId},
                        {legacyTopicId},
                        CONVERT(int, COUNT_BIG(*)),
                        {updatedAt}
                    FROM dbo.ModernForumPost
                    WHERE ThreadId = {threadId};
                END;
            END;
            """, cancellationToken);
    }

    private bool IsSqlServer() =>
        string.Equals(dbContext.Database.ProviderName, "Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal);

    private static DateTime ToUtcDateTime(DateTimeOffset value) =>
        value.UtcDateTime;

    private static DateTimeOffset ToOffset(DateTime? value) =>
        value.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
            : DateTimeOffset.MinValue;

    private static string TruncateBody(string body) =>
        body.Length <= BodyHtmlMaxLength ? body : body[..BodyHtmlMaxLength];
}
