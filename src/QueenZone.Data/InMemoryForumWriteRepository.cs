namespace QueenZone.Data;

public sealed class InMemoryForumWriteRepository : IForumWriteRepository
{
    private readonly List<ForumWriteThread> threads = [];
    private readonly List<InMemoryForumWritePost> posts = [];
    private readonly object sync = new();
    private int nextTopicId = 200_000;
    private int nextPostId = 2_000_000;
    private InMemoryForumPollRepository? pollRepository;

    public void AttachPollRepository(InMemoryForumPollRepository repository) =>
        pollRepository = repository;

    public async Task<ForumThreadCreateResult> CreateThreadAsync(NewForumThread thread, CancellationToken cancellationToken = default)
    {
        int topicId;
        int postId;
        lock (sync)
        {
            topicId = nextTopicId++;
            postId = nextPostId++;
            threads.Add(new ForumWriteThread(
                topicId,
                thread.CategoryId,
                thread.Subject.Trim(),
                thread.CreatedAt,
                thread.CreatedAt,
                1,
                IsLocked: false));
            posts.Add(new InMemoryForumWritePost(
                postId,
                topicId,
                thread.AuthorMemberId,
                thread.AuthorDisplayName,
                thread.Body,
                thread.CreatedAt));
        }

        if (thread.Poll is not null && pollRepository is not null)
        {
            pollRepository.RegisterTopic(topicId);
            await pollRepository.CreatePollAsync(
                topicId,
                thread.Poll with { CreatedByMemberId = thread.AuthorMemberId },
                cancellationToken);
        }

        return new ForumThreadCreateResult(topicId, postId);
    }

    public Task<int> CreatePostAsync(NewForumPost post, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var index = threads.FindIndex(thread => thread.TopicId == post.TopicId);
            if (index < 0)
            {
                var header = SampleForumData.TryGetSeedTopicHeader(post.TopicId)
                    ?? throw new InvalidOperationException("Forum thread not found.");
                threads.Add(new ForumWriteThread(
                    post.TopicId,
                    header.ForumId,
                    header.Title,
                    post.CreatedAt,
                    post.CreatedAt,
                    SampleForumData.CreateSeedPosts(post.TopicId).Count,
                    IsLocked: false));
                index = threads.Count - 1;
            }

            var thread = threads[index];
            if (thread.IsLocked)
            {
                throw new InvalidOperationException("Forum thread is locked.");
            }

            var postId = nextPostId++;
            posts.Add(new InMemoryForumWritePost(
                postId,
                post.TopicId,
                post.AuthorMemberId,
                post.AuthorDisplayName,
                post.Body,
                post.CreatedAt));
            threads[index] = thread with
            {
                LastPostAt = post.CreatedAt,
                PostCount = thread.PostCount + 1,
            };
            return Task.FromResult(postId);
        }
    }

    public Task<ForumEditablePost?> GetPostAsync(int postId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var post = posts.SingleOrDefault(item => item.PostId == postId);
            if (post is null)
            {
                return Task.FromResult<ForumEditablePost?>(null);
            }

            var subject = threads.SingleOrDefault(thread => thread.TopicId == post.TopicId)?.Subject
                ?? SampleForumData.TryGetSeedTopicHeader(post.TopicId)?.Title
                ?? string.Empty;

            var seedCount = SampleForumData.CreateSeedPosts(post.TopicId).Count;
            var createdBefore = posts.Count(item => item.TopicId == post.TopicId && item.PostId <= postId);
            // Created posts append after seed posts for topics that started from sample data.
            var position = seedCount > 0 && threads.Any(thread => thread.TopicId == post.TopicId)
                ? seedCount + createdBefore
                : createdBefore;

            return Task.FromResult<ForumEditablePost?>(new ForumEditablePost(
                post.PostId,
                post.TopicId,
                subject,
                post.Body,
                post.MemberId,
                post.CreatedAt,
                post.EditedAt,
                post.EditCount,
                Math.Max(1, position)));
        }
    }

    public Task<ForumPostUpdateResult> UpdatePostAsync(
        int postId,
        Guid editorMemberId,
        string sanitisedBody,
        bool isAdmin,
        int editWindowMinutes,
        CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var index = posts.FindIndex(item => item.PostId == postId);
            if (index < 0)
            {
                return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.NotFound));
            }

            var post = posts[index];
            var subject = threads.SingleOrDefault(thread => thread.TopicId == post.TopicId)?.Subject
                ?? SampleForumData.TryGetSeedTopicHeader(post.TopicId)?.Title
                ?? string.Empty;
            var utcNow = DateTimeOffset.UtcNow;
            var canEdit = ForumPostEditRules.CanEdit(
                post.MemberId,
                editorMemberId,
                isAdmin,
                post.CreatedAt,
                editWindowMinutes,
                utcNow);

            if (!canEdit)
            {
                if (!isAdmin && post.MemberId == editorMemberId && editWindowMinutes == 0)
                {
                    return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.EditingDisabled, post.TopicId, subject));
                }

                if (!isAdmin
                    && post.MemberId == editorMemberId
                    && editWindowMinutes > 0
                    && utcNow > post.CreatedAt.AddMinutes(editWindowMinutes))
                {
                    return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.EditWindowExpired, post.TopicId, subject));
                }

                return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.Forbidden, post.TopicId, subject));
            }

            posts[index] = post with
            {
                Body = sanitisedBody,
                EditedAt = utcNow,
                EditCount = post.EditCount + 1,
            };

            return Task.FromResult(new ForumPostUpdateResult(ForumPostUpdateStatus.Success, post.TopicId, subject));
        }
    }

    public Task<ForumWriteThread?> GetThreadAsync(int topicId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            var thread = threads.SingleOrDefault(thread => thread.TopicId == topicId);
            if (thread is not null)
            {
                return Task.FromResult<ForumWriteThread?>(thread);
            }

            var header = SampleForumData.TryGetSeedTopicHeader(topicId);
            return Task.FromResult<ForumWriteThread?>(header is null
                ? null
                : new ForumWriteThread(
                    topicId,
                    header.ForumId,
                    header.Title,
                    DateTimeOffset.MinValue,
                    DateTimeOffset.MinValue,
                    SampleForumData.CreateSeedPosts(topicId).Count,
                    IsLocked: false));
        }
    }

    public Task<int> CountPostsByMemberSinceAsync(Guid memberId, DateTimeOffset since, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(posts.Count(post => post.MemberId == memberId && post.CreatedAt >= since));
        }
    }

    public Task<int> CountApprovedPostsByMemberAsync(Guid memberId, CancellationToken cancellationToken = default)
    {
        lock (sync)
        {
            return Task.FromResult(posts.Count(post => post.MemberId == memberId));
        }
    }

    public IReadOnlyList<ForumWriteThread> GetCreatedThreads()
    {
        lock (sync)
        {
            return threads.ToList();
        }
    }

    public IReadOnlyList<InMemoryForumWritePost> GetPostsForTopic(int topicId)
    {
        lock (sync)
        {
            return posts.Where(post => post.TopicId == topicId).ToList();
        }
    }
}

public sealed record InMemoryForumWritePost(
    int PostId,
    int TopicId,
    Guid MemberId,
    string DisplayName,
    string Body,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EditedAt = null,
    int EditCount = 0);
